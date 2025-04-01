using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Localization;
using Terraria.DataStructures;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

[ApiVersion(2, 1)]
public class MultiWeapon : TerrariaPlugin
{
    private Dictionary<int, Vector2> playerMousePositions = new Dictionary<int, Vector2>();

    public MultiWeapon(Main game) : base(game) { }
    public override string Name => "MultiWeapon";
    public override Version Version => new Version(1, 3);
    public override string Author => "YourName";

    public override void Initialize()
    {
        GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
        GetDataHandlers.PlayerAnimation.Register(OnPlayerAnimation);
    }

    private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        playerMousePositions[args.Player.Index] = args.Position;
    }

    private void OnPlayerAnimation(object sender, GetDataHandlers.PlayerAnimationEventArgs args)
    {
        using (var reader = new BinaryReader(args.Data))
        {
            try
            {
                byte playerId = reader.ReadByte(); // Baca Player ID
                byte animationType = reader.ReadByte();

                if (animationType == 1) // Animasi serangan
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
        Item mainWeapon = player.TPlayer.inventory[mainSlot];

        for (int slot = 0; slot < 3; slot++)
        {
            if (slot == mainSlot)
                continue;

            Item weapon = player.TPlayer.inventory[slot];
            if (IsValidWeapon(weapon))
            {
                SimulateWeaponAttack(player, slot, weapon, mousePos);
            }
        }
    }

    private void SimulateWeaponAttack(TSPlayer player, int slot, Item weapon, Vector2 mousePos)
    {
        int originalSlot = player.TPlayer.selectedItem;
        try
        {
            // 1. Ubah slot sementara
            player.TPlayer.selectedItem = slot;
            
            // 2. Hitung arah serangan
            Vector2 attackDirection = (mousePos - player.TPlayer.Center).SafeNormalize(Vector2.UnitX);
            player.TPlayer.direction = attackDirection.X > 0 ? 1 : -1;

            // 3. Eksekusi serangan menggunakan Projectile
            Projectile.NewProjectile(
                new EntitySource_ItemUse(player.TPlayer, weapon),
                player.TPlayer.Center,
                attackDirection * weapon.shootSpeed,
                weapon.shoot,
                weapon.damage,
                weapon.knockBack,
                player.Index
            );

            // 4. Update animasi
            NetMessage.SendData(
                41,
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
               item.pick == 0 && 
               item.axe == 0 && 
               item.hammer == 0;
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
