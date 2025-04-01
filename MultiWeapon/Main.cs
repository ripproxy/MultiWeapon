using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

[ApiVersion(2, 1)]
public class SyncedAttack : TerrariaPlugin
{
    private Dictionary<int, Vector2> attackDirections = new Dictionary<int, Vector2>();

    public override void Initialize()
    {
        GetDataHandlers.PlayerUpdate.Register(this, OnPlayerUpdate);
        GetDataHandlers.PlayerAnimation.Register(this, OnPlayerAnimation); // Handler khusus untuk Packet 41
    }

    // Handler untuk Packet 41 (PlayerAnimation)
    private void OnPlayerAnimation(object sender, GetDataHandlers.PlayerAnimationEventArgs args)
    {
        using (var reader = new BinaryReader(new MemoryStream(args.Data)))
        {
            try
            {
                int playerId = reader.ReadByte();
                byte animationType = reader.ReadByte();

                if (animationType == 1) // Animasi mulai (misalnya ayunan senjata)
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
        args.Handled = false; // Biarkan packet diproses lebih lanjut
    }

    private void ProcessAttack(TSPlayer player, int mainSlot)
    {
        if (player == null || mainSlot < 0 || mainSlot > 2)
            return;

        if (!attackDirections.TryGetValue(player.Index, out Vector2 mousePos))
            return;

        // Proses 3 slot senjata
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
            GetDataHandlers.PlayerAnimation.UnRegister(this, OnPlayerAnimation); // Unregister handler yang benar
        }
        base.Dispose(disposing);
    }

    // Wajib diimplementasikan (constructor dan metadata)
    public SyncedAttack(Main game) : base(game) { }
    public override string Name => "SyncedAttack";
    public override Version Version => new Version(1, 0);
}
