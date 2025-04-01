using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;

[ApiVersion(2, 1)]
public class SyncedAttack : TerrariaPlugin
{
    private Dictionary<int, Vector2> attackDirections = new Dictionary<int, Vector2>();
    
    public override string Author => "Nama Anda";
    public override string Description => "3 senjata dengan arah serangan sama";
    public override string Name => "SyncedAttack";
    public override Version Version => new Version(1, 3, 0);

    public SyncedAttack(Main game) : base(game) { }

    public override void Initialize()
    {
        GetDataHandlers.PlayerUpdate.Register(this, OnPlayerUpdate);
        GetDataHandlers.PlayerItemAnimation.Register(this, OnItemAnimation);
    }

    private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        var player = TShock.Players[args.PlayerId];
        if (player == null || !args.Control.IsUsingItem)
            return;

        attackDirections[args.PlayerId] = new Vector2(args.MouseX, args.MouseY);
    }

    private void OnItemAnimation(GetDataHandlers.PlayerItemAnimationEventArgs args)
    {
        var player = TShock.Players[args.PlayerId];
        if (player == null || args.Type != 1)
            return;

        if (!attackDirections.TryGetValue(args.PlayerId, out Vector2 mousePos))
            return;

        int mainSlot = player.TPlayer.selectedItem;
        if (mainSlot < 0 || mainSlot > 2)
            return;

        for (int slot = 0; slot < 3; slot++)
        {
            if (slot == mainSlot)
                continue;

            Item weapon = player.TPlayer.inventory[slot];
            if (IsValidWeapon(weapon))
            {
                SyncAttackDirection(player, slot, weapon, mousePos);
            }
        }
    }

    private void SyncAttackDirection(TSPlayer player, int slot, Item weapon, Vector2 mousePos)
    {
        Vector2 worldMouse = new Vector2(
            player.TPlayer.position.X + mousePos.X - Main.screenWidth / 2,
            player.TPlayer.position.Y + mousePos.Y - Main.screenHeight / 2
        );

        int originalSlot = player.TPlayer.selectedItem;
        try
        {
            player.TPlayer.selectedItem = slot;
            player.TPlayer.direction = (worldMouse.X > player.TPlayer.Center.X) ? 1 : -1;
            
            // Proses serangan
            weapon.UseItem(player.Index);

            // Update animasi ke client
            NetMessage.SendData(
                (int)PacketTypes.PlayerItemAnimation,
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
               !item.notAmmo && 
               item.pick == 0 && 
               item.axe == 0 && 
               item.hammer == 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GetDataHandlers.PlayerUpdate.UnRegister(this, OnPlayerUpdate);
            GetDataHandlers.PlayerItemAnimation.UnRegister(this, OnItemAnimation);
        }
        base.Dispose(disposing);
    }
}
