using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

[ApiVersion(2, 1)]
public class MultiWeapon : TerrariaPlugin
{
    private Dictionary<int, Vector2> playerMousePositions = new Dictionary<int, Vector2>();

    public MultiWeapon(Main game) : base(game) { }
    public override string Name => "MultiWeapon";
    public override Version Version => new Version(3, 0);
    public override string Author => "YourName";

    public override void Initialize()
    {
        GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
        GetDataHandlers.PlayerAnimation.Register(OnPlayerAnimation);
    }

    private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        // Simpan posisi mouse pemain
        playerMousePositions[args.Player.Index] = args.Position;
    }

    private void OnPlayerAnimation(object sender, GetDataHandlers.PlayerAnimationEventArgs args)
    {
        using (var reader = new BinaryReader(args.Data))
        {
            try
            {
                byte playerId = reader.ReadByte();
                byte animationType = reader.ReadByte();

                if (animationType == 1) // Pastikan hanya trigger saat serangan
                {
                    var player = TShock.Players[playerId];
                    if (player != null && playerMousePositions.TryGetValue(player.Index, out Vector2 mousePos))
                    {
                        ExecuteMultiAttack(player, mousePos);
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }
        args.Handled = false;
    }

    private void ExecuteMultiAttack(TSPlayer player, Vector2 mousePos)
    {
        int mainSlot = player.TPlayer.selectedItem;

        // Proses 3 slot secara paralel
        for (int slot = 0; slot < 3; slot++)
        {
            if (slot == mainSlot)
                continue;

            Item weapon = player.TPlayer.inventory[slot];
            if (IsValidWeapon(weapon))
            {
                if (weapon.melee)
                    SimulateMeleeAttack(player, slot, weapon, mousePos);
                else
                    SimulateProjectileAttack(player, slot, weapon, mousePos);
            }
        }
    }

    private void SimulateMeleeAttack(TSPlayer player, int slot, Item weapon, Vector2 mousePos)
    {
        int originalSlot = player.TPlayer.selectedItem;
        try
        {
            player.TPlayer.selectedItem = slot;
            Vector2 attackDirection = (mousePos - player.TPlayer.Center).SafeNormalize(Vector2.UnitX);
            player.TPlayer.direction = attackDirection.X > 0 ? 1 : -1;

            // Hitung area serangan berdasarkan ukuran senjata
            Rectangle hitbox = new Rectangle(
                (int)(player.TPlayer.Center.X - weapon.width),
                (int)(player.TPlayer.Center.Y - weapon.height),
                weapon.width * 2,
                weapon.height * 2
            );

            // Beri damage ke semua NPC dalam area
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && hitbox.Intersects(npc.getRect()))
                {
                    npc.StrikeNPC(
                        weapon.damage,
                        weapon.knockBack,
                        player.TPlayer.direction,
                        crit: false
                    );
                }
            }

            // Paksa update animasi ke semua client
            NetMessage.SendData(
                (int)PacketTypes.PlayerAnimation,
                -1, -1,
                NetworkText.Empty,
                player.Index,
                slot,
                1
            );
        }
        finally
        {
            player.TPlayer.selectedItem = originalSlot;
        }
    }

    private void SimulateProjectileAttack(TSPlayer player, int slot, Item weapon, Vector2 mousePos)
    {
        int originalSlot = player.TPlayer.selectedItem;
        try
        {
            player.TPlayer.selectedItem = slot;
            Vector2 attackDirection = (mousePos - player.TPlayer.Center).SafeNormalize(Vector2.UnitX);
            player.TPlayer.direction = attackDirection.X > 0 ? 1 : -1;

            // Tembakkan projectile
            Projectile.NewProjectile(
                new EntitySource_ItemUse(player.TPlayer, weapon),
                player.TPlayer.Center,
                attackDirection * weapon.shootSpeed,
                weapon.shoot,
                weapon.damage,
                weapon.knockBack,
                player.Index
            );

            // Update animasi
            NetMessage.SendData(
                (int)PacketTypes.PlayerAnimation,
                -1, -1,
                NetworkText.Empty,
                player.Index,
                slot,
                1
            );
        }
        finally
        {
            player.TPlayer.selectedItem = originalSlot;
        }
    }

    private bool IsValidWeapon(Item item)
    {
        return item.active && 
               item.damage > 0 && 
               (item.melee || item.ranged || item.magic || item.summon || item.thrown);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GetDataHandlers.PlayerUpdate.UnRegister(OnPlayerUpdate);
            GetDataHandlers.PlayerAnimation.UnRegister(OnPlayerAnimation);
        }
        base.Dispose(disposing);
    }
}
