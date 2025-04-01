using System;
using Microsoft.Xna.Framework;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Terraria.Localization;

namespace MultiWeaponPlugin
{
    [ApiVersion(2, 1)]
    public class MultiWeaponPlugin : TerrariaPlugin
    {
        public override string Name => "MultiWeaponPlugin";
        public override string Author => "YourName";
        public override string Description => "Plugin untuk menyerang dengan 3 senjata sekaligus";
        public override Version Version => new Version(1, 0, 1);

        private readonly int[] weaponSlots = { 0, 1, 2 };
        private const int DefaultMeleeProj = 15; // ID proyektil untuk slash pedang dasar

        public MultiWeaponPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled || (int)args.MsgID != 41)
                return;

            int playerIndex = args.Msg.whoAmI;
            TSPlayer tsPlayer = TShock.Players[playerIndex];
            if (tsPlayer?.Active != true)
                return;

            int selectedSlot = tsPlayer.TPlayer.selectedItem;
            if (Array.IndexOf(weaponSlots, selectedSlot) == -1)
                return;

            Item triggerItem = tsPlayer.TPlayer.inventory[selectedSlot];
            if (triggerItem?.damage <= 0)
                return;

            foreach (int slot in weaponSlots)
            {
                if (slot == selectedSlot)
                    continue;

                Item weaponItem = tsPlayer.TPlayer.inventory[slot];
                if (weaponItem?.damage <= 0)
                    continue;

                // ========== PERUBAHAN UTAMA ==========
                int projType = weaponItem.shoot;
                bool isMelee = weaponItem.melee;

                // Handle senjata melee tanpa proyektil
                if (isMelee && projType == 0)
                    projType = DefaultMeleeProj;

                // Skip jika tetap tidak ada proyektil
                if (projType == 0)
                    continue;
                // =====================================

                Vector2 pos = tsPlayer.TPlayer.Center;
                float speed = 10f;
                float rotation = tsPlayer.TPlayer.itemRotation;
                if (tsPlayer.TPlayer.direction == -1)
                    rotation += MathHelper.Pi;

                Vector2 velocity = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * speed;
                int damage = Math.Max(weaponItem.damage, 10);
                float knockBack = Math.Max(weaponItem.knockBack, 2f);

                // Generate proyektil
                int projId = Projectile.NewProjectile(
                    null,
                    pos.X,
                    pos.Y,
                    velocity.X,
                    velocity.Y,
                    projType,
                    damage,
                    knockBack,
                    tsPlayer.Index
                );

                NetMessage.SendData(
                    (int)PacketTypes.ProjectileNew,
                    -1,
                    -1,
                    NetworkText.FromLiteral(""),
                    projId,
                    0f, 0f, 0f, 0
                );
            }
        }
    }
}
