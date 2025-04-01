using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace MultiWeapon
{
    [ApiVersion(2, 1)]
    public class MultiWeapon : TerrariaPlugin
    {
        public override string Name => "MultiWeapon";
        public override Version Version => new Version(1, 0);
        public override string Author => "YourName";
        public override string Description => "Allows simultaneous weapon attacks from slots 0-2";

        public MultiWeapon(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            PlayerHooks.PlayerPostUpdate += OnPlayerPostUpdate;
        }

        private void OnPlayerPostUpdate(PlayerPostUpdateEventArgs e)
        {
            if (e.Player == null || !e.Player.Active || e.Player.SelectedItem < 0 || e.Player.SelectedItem > 2)
                return;

            var player = e.Player.TPlayer;
            var tsPlayer = TShock.Players[e.Player.Index];

            if (player.controlUseItem && player.itemAnimation == 0 && player.releaseUseItem)
            {
                var originalSlot = player.selectedItem;
                var originalDirection = player.direction;

                for (int i = 0; i < 3; i++)
                {
                    var item = player.inventory[i];
                    if (item.damage > 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0)
                    {
                        player.selectedItem = i;
                        player.direction = originalDirection;
                        player.itemTime = 0;
                        player.ItemCheck();
                    }
                }

                player.selectedItem = originalSlot;
                tsPlayer.SendData(PacketTypes.PlayerSlot, "", player.whoAmI, originalSlot, 0);
                tsPlayer.SendData(PacketTypes.PlayerControls, "", player.whoAmI);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlayerHooks.PlayerPostUpdate -= OnPlayerPostUpdate;
            }
            base.Dispose(disposing);
        }
    }
}
