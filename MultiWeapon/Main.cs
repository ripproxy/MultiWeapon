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
    public override Version Version => new Version(1, 3);
    public override string Author => "YourName";

    public override void Initialize()
    {
        GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
        GetDataHandlers.PlayerAnimation.Register(OnPlayerAnimation);
    }

    // Simpan posisi mouse pemain
    private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        playerMousePositions[args.Player.Index] = args.Position;
    }

    // Tangkap packet animasi serangan
    private void OnPlayerAnimation(object sender, GetDataHandlers.PlayerAnimationEventArgs args)
    {
        if (args.AnimationType == 1) // Animasi serangan
        {
            var player = TShock.Players[args.PlayerId];
            if (player != null && playerMousePositions.TryGetValue(player.Index, out Vector2 mousePos))
            {
                ExecuteMultiAttack(player, mousePos);
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
        // 1. Simpan state asli
        int originalSlot = player.TPlayer.selectedItem;
        Item originalHeldItem = player.TPlayer.HeldItem;

        try
        {
            // 2. Ubah slot sementara
            player.TPlayer.selectedItem = slot;
            player.TPlayer.HeldItem = weapon;

            // 3. Hitung arah serangan
            Vector2 attackDirection = (mousePos - player.TPlayer.Center).SafeNormalize(Vector2.UnitX);
            player.TPlayer.direction = attackDirection.X > 0 ? 1 : -1;

            // 4. Eksekusi logika serangan
            weapon.UseItem(
                player.Index,
                new EntitySource_ItemUse(player.TPlayer, weapon),
                player.TPlayer.Center,
                attackDirection * weapon.shootSpeed,
                weapon.shoot,
                weapon.damage,
                weapon.knockBack
            );

            // 5. Update animasi ke client
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
            // 6. Restore state asli
            player.TPlayer.selectedItem = originalSlot;
            player.TPlayer.HeldItem = originalHeldItem;
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
