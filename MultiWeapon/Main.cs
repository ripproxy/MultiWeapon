using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

[ApiVersion(2, 1)]
public class SyncedAttack : TerrariaPlugin
{
    private Dictionary<int, Vector2> attackDirections = new Dictionary<int, Vector2>();

    public SyncedAttack(Main game) : base(game) { }
    public override string Name => "SyncedAttack";
    public override Version Version => new Version(1, 0);
    public override string Author => "YourName";

    public override void Initialize()
    {
        GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
        GetDataHandlers.PlayerAnimation.Register(OnPlayerAnimation);
    }

    // Handler untuk PlayerUpdate (wajib ada)
    private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        // Implementasi sesuai kebutuhan
    }

    // Handler untuk PlayerAnimation (Packet 41)
    private void OnPlayerAnimation(object sender, GetDataHandlers.PlayerAnimationEventArgs args)
    {
        using (var reader = new BinaryReader(new MemoryStream(args.Data)))
        {
            try
            {
                int playerId = reader.ReadByte();
                byte animationType = reader.ReadByte();

                if (animationType == 1)
                {
                    var player = TShock.Players[playerId];
                    if (player != null)
                    {
                        ProcessAttack(player, player.TPlayer.selectedItem);
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

    private void ProcessAttack(TSPlayer player, int mainSlot)
    {
        if (player == null || mainSlot < 0 || mainSlot > 2)
            return;

        // Proses 3 slot senjata
        for (int slot = 0; slot < 3; slot++)
        {
            if (slot == mainSlot)
                continue;

            Item weapon = player.TPlayer.inventory[slot];
            if (IsValidWeapon(weapon))
            {
                SyncAttackDirection(player, slot, weapon);
            }
        }
    }

    private void SyncAttackDirection(TSPlayer player, int slot, Item weapon)
    {
        int originalSlot = player.TPlayer.selectedItem;
        try
        {
            player.TPlayer.selectedItem = slot;
            
            // Gunakan item dengan cara yang valid
            player.TPlayer.HeldItem = weapon;
            player.TPlayer.ItemCheck();

            // Update animasi ke client
            NetMessage.SendData(
                (int)PacketTypes.PlayerControls,
                -1, -1,
                NetworkText.Empty,
                player.Index
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
            GetDataHandlers.PlayerUpdate.UnRegister(OnPlayerUpdate);
            GetDataHandlers.PlayerAnimation.UnRegister(OnPlayerAnimation);
        }
        base.Dispose(disposing);
    }
}
