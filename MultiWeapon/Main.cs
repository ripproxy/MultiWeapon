using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

[ApiVersion(2, 1)]
public class MultiWeapon : TerrariaPlugin
{
    public override string Name => "MultiWeapon";
    public override string Author => "ripproxy";
    public override string Description => "Serangan bersamaan dari beberapa slot hotbar dengan cooldown, mana, dan ammo per item.";
    public override Version Version => new Version(1, 4, 0);

    private static string ConfigPath => Path.Combine(TShock.SavePath, "MultiWeapon.json");
    private Config _config = new();

    // Cooldown per player per slot
    private readonly Dictionary<int, int[]> _cooldowns = new();

    public MultiWeapon(Main game) : base(game) { }

    public override void Initialize()
    {
        LoadConfig();
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.ServerLeave.Register(this, OnPlayerLeave);
        Commands.ChatCommands.Add(new Command("multiweapon.reload", ReloadCommand, "mwreload")
        {
            HelpText = "Reload MultiWeapon config"
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnPlayerLeave);
        }
        base.Dispose(disposing);
    }

    private void OnPlayerLeave(LeaveEventArgs args)
    {
        _cooldowns.Remove(args.Who);
    }

    private void ReloadCommand(CommandArgs args)
    {
        LoadConfig();
        args.Player.SendSuccessMessage("[MultiWeapon] Config reloaded.");
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                _config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
            }
            else
            {
                _config = new Config();
                SaveConfig();
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError("[MultiWeapon] Failed to load config: " + ex.Message);
            _config = new Config();
        }
    }

    private void SaveConfig()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError("[MultiWeapon] Failed to save config: " + ex.Message);
        }
    }

    private void OnGameUpdate(EventArgs args)
    {
        if (!_config.Enabled) return;

        foreach (TSPlayer? tsPlayer in TShock.Players)
        {
            if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null)
                continue;

            Player player = tsPlayer.TPlayer;
            int index = tsPlayer.Index;

            if (!_cooldowns.TryGetValue(index, out int[] cds))
            {
                cds = new int[10];
                _cooldowns[index] = cds;
            }

            // Turunkan cooldown setiap frame
            for (int i = 0; i < cds.Length; i++)
            {
                if (cds[i] > 0) cds[i]--;
            }

            if (!player.controlUseItem) continue;

            // Optional: hanya aktif kalau lagi pegang slot 0
            if (_config.RequireSelectedSlot0 && player.selectedItem != 0)
                continue;

            // Tentukan item acuan untuk cooldown (kalau mode bukan Self)
            Item? referenceItem = null;
            string mode = (_config.CooldownMode ?? "Self").Trim().ToLowerInvariant();

            if (mode == "slot0")
            {
                referenceItem = player.inventory[0];
            }
            else if (mode == "held")
            {
                referenceItem = player.inventory[player.selectedItem];
            }

            foreach (int slot in _config.ExtraSlots)
            {
                if (slot < 0 || slot >= player.inventory.Length) continue;
                if (cds[slot] > 0) continue;

                Item weapon = player.inventory[slot];
                if (weapon == null || weapon.IsAir || weapon.damage <= 0) continue;

                // ===== Cek Mana =====
                if (_config.CheckMana && weapon.mana > 0)
                {
                    if (player.statMana < weapon.mana) continue;
                }

                // ===== Cek Ammo =====
                if (_config.CheckAmmo && weapon.useAmmo > 0)
                {
                    if (!HasAmmo(player, weapon.useAmmo)) continue;
                }

                // ===== MELEE =====
                if (weapon.melee && !weapon.noMelee)
                {
                    player.itemAnimation = weapon.useAnimation;
                    player.itemAnimationMax = weapon.useAnimation;
                    player.itemTime = weapon.useTime;
                    NetMessage.SendData((int)PacketTypes.PlayerAnimation, -1, -1, null, index);
                }
                // ===== PROJECTILE =====
                else if (weapon.shoot > 0)
                {
                    Vector2 position = player.Center;
                    Vector2 velocity;

                    if (_config.UsePlayerAim)
                    {
                        float rotation = player.itemRotation;
                        if (player.direction == -1)
                            rotation += MathHelper.Pi;

                        velocity = rotation.ToRotationVector2() * weapon.shootSpeed;
                    }
                    else
                    {
                        velocity = new Vector2(player.direction, 0f) * weapon.shootSpeed;
                    }

                    position += Vector2.Normalize(velocity) * 20f;

                    int projIndex = Projectile.NewProjectile(
                        null,
                        position,
                        velocity,
                        weapon.shoot,
                        weapon.damage,
                        weapon.knockBack,
                        index
                    );

                    if (projIndex >= 0 && projIndex < Main.maxProjectiles)
                    {
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projIndex);
                    }

                    // Consume mana
                    if (_config.CheckMana && weapon.mana > 0)
                    {
                        player.statMana = Math.Max(0, player.statMana - weapon.mana);
                        player.manaRegenDelay = (int)player.maxRegenDelay;
                    }

                    // Consume ammo
                    if (_config.CheckAmmo && weapon.useAmmo > 0)
                    {
                        ConsumeAmmo(player, weapon.useAmmo);
                    }
                }

                // ===== Set Cooldown sesuai mode =====
                int baseUseTime;

                if (mode == "self" || referenceItem == null || referenceItem.IsAir)
                {
                    // Tiap senjata pakai useTime sendiri
                    baseUseTime = weapon.useTime;
                }
                else
                {
                    // Ikuti useTime item acuan (Slot0 atau Held)
                    baseUseTime = referenceItem.useTime > 0 ? referenceItem.useTime : weapon.useTime;
                }

                int cd = (int)(baseUseTime * _config.CooldownMultiplier);
                cds[slot] = Math.Max(cd, 1);
            }
        }
    }

    private bool HasAmmo(Player player, int ammoType)
    {
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item != null && !item.IsAir && item.ammo == ammoType && item.stack > 0)
                return true;
        }
        return false;
    }

    private void ConsumeAmmo(Player player, int ammoType)
    {
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item != null && !item.IsAir && item.ammo == ammoType && item.stack > 0)
            {
                item.stack--;
                if (item.stack <= 0)
                    item.TurnToAir();

                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, player.whoAmI, i);
                break;
            }
        }
    }

    public class Config
    {
        public bool Enabled { get; set; } = true;

        /// <summary>Hanya aktif jika player sedang memegang slot 0</summary>
        public bool RequireSelectedSlot0 { get; set; } = true;

        /// <summary>Slot tambahan yang ikut menyerang</summary>
        public int[] ExtraSlots { get; set; } = new[] { 1, 2 };

        /// <summary>Cek & kurangi mana sesuai item</summary>
        public bool CheckMana { get; set; } = true;

        /// <summary>Cek & kurangi ammo sesuai item</summary>
        public bool CheckAmmo { get; set; } = true;

        /// <summary>Ikuti arah aim / rotasi item player</summary>
        public bool UsePlayerAim { get; set; } = true;

        /// <summary>
        /// Mode cooldown:
        /// "Self"  = tiap senjata pakai useTime sendiri (default)
        /// "Slot0" = semua extra slot ikut cooldown item di slot 0
        /// "Held"  = semua extra slot ikut cooldown item yang sedang dipegang
        /// </summary>
        public string CooldownMode { get; set; } = "Self";

        /// <summary>Pengali cooldown (1.0 = normal)</summary>
        public float CooldownMultiplier { get; set; } = 1.0f;
    }
}
