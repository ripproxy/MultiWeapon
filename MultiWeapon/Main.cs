using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

[ApiVersion(2, 1)]
public class MultiWeapon : TerrariaPlugin
{
    public override string Name => "MultiWeapon";
    public override string Author => "ripproxy";
    public override string Description => "Memungkinkan serangan bersamaan dari slot 0, 1, dan 2 dengan cooldown masing-masing.";
    public override Version Version => new Version(1, 2, 0);

    // Cooldown per player: Key = player.Index, Value = array[3] untuk slot 0,1,2
    private readonly Dictionary<int, int[]> _cooldowns = new();

    public MultiWeapon(Main game) : base(game)
    {
    }

    public override void Initialize()
    {
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.ServerLeave.Register(this, OnPlayerLeave); // bersihkan data saat player keluar
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

    private void OnGameUpdate(EventArgs args)
    {
        foreach (TSPlayer? tsPlayer in TShock.Players)
        {
            if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null)
                continue;

            Player player = tsPlayer.TPlayer;
            int index = tsPlayer.Index;

            // Inisialisasi cooldown array jika belum ada
            if (!_cooldowns.TryGetValue(index, out int[] cds))
            {
                cds = new int[3];
                _cooldowns[index] = cds;
            }

            // Kurangi semua cooldown setiap frame
            for (int i = 0; i < 3; i++)
            {
                if (cds[i] > 0)
                    cds[i]--;
            }

            // Hanya proses jika player sedang menahan tombol use item
            // dan sedang memilih slot 0
            if (!player.controlUseItem || player.selectedItem != 0)
                continue;

            Item mainWeapon = player.inventory[0];
            if (mainWeapon == null || mainWeapon.IsAir || mainWeapon.damage <= 0)
                continue;

            // Proses slot 1 dan 2
            for (int slot = 1; slot <= 2; slot++)
            {
                // Masih dalam cooldown?
                if (cds[slot] > 0)
                    continue;

                Item weapon = player.inventory[slot];
                if (weapon == null || weapon.IsAir || weapon.damage <= 0)
                    continue;

                // ======================
                //  MELEE
                // ======================
                if (weapon.melee)
                {
                    player.itemAnimation = weapon.useAnimation;
                    player.itemAnimationMax = weapon.useAnimation;
                    player.itemTime = weapon.useTime;

                    NetMessage.SendData((int)PacketTypes.PlayerAnimation, -1, -1, null, index);
                }
                // ======================
                //  PROJECTILE (ranged / magic / summon)
                // ======================
                else if (weapon.shoot > 0)
                {
                    Vector2 position = player.Center + new Vector2(player.direction * 20f, 0f);
                    Vector2 velocity = new Vector2(player.direction, 0f) * weapon.shootSpeed;

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
                }

                // Set cooldown sesuai useTime senjata
                cds[slot] = Math.Max(weapon.useTime, 1);
            }
        }
    }
}
