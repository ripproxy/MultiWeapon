using System;
using Microsoft.Xna.Framework; // Untuk Vector2
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
        public override string Description => "Plugin yang mendeteksi serangan senjata di slot tertentu dan men-trigger serangan tambahan dari senjata di slot lain (misalnya, slot hotbar indeks 0, 1, 2).";
        public override Version Version => new Version(1, 0, 0);

        // Tentukan slot senjata yang akan dipantau. Di sini kita gunakan tiga slot pertama (array 0-indexed)
        private readonly int[] weaponSlots = { 0, 1, 2 };

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
            if (args.Handled)
                return;

            // Deteksi paket yang menandakan serangan senjata.
            if ((int)args.MsgID == 41)
            {
                int playerIndex = args.Msg.whoAmI;
                TSPlayer tsPlayer = TShock.Players[playerIndex];
                if (tsPlayer == null || !tsPlayer.Active)
                    return;

                // Ambil slot item yang dipakai pemain
                int selectedSlot = tsPlayer.TPlayer.selectedItem;

                // Pastikan bahwa senjata utama (yang trigger) bisa berupa apa saja.
                // Tetapi kita hanya akan melakukan trigger jika senjata utama berada di salah satu slot yang ditentukan.
                if (Array.IndexOf(weaponSlots, selectedSlot) == -1)
                    return;

                // Periksa item pada slot yang aktif
                Item triggerItem = tsPlayer.TPlayer.inventory[selectedSlot];
                if (triggerItem == null || triggerItem.damage <= 0)
                    return; // Senjata trigger tidak valid (misalnya, bukan senjata)

                // Untuk setiap slot di weaponSlots selain slot trigger, ambil properti dari item di slot tersebut.
                foreach (int slot in weaponSlots)
                {
                    if (slot == selectedSlot)
                        continue; // Lewati senjata trigger

                    Item weaponItem = tsPlayer.TPlayer.inventory[slot];
                    // Pastikan item tersebut valid dan memiliki damage > 0 serta memiliki properti shoot (tipe projectile)
                    if (weaponItem != null && weaponItem.damage > 0)
                    {
                        // Dapatkan posisi pemain sebagai titik awal projectile
                        Vector2 pos = tsPlayer.TPlayer.position;
                        
                        float speed = 10f;
                        float rotation = tsPlayer.TPlayer.itemRotation;
                        if (tsPlayer.TPlayer.direction == -1)
                        rotation += (float)Math.PI;

                        Vector2 velocity = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * speed;

                        // Gunakan properti dari item di slot tambahan
                        int projType = weaponItem.shoot; // Tipe projectile sesuai senjata
                        int damage = weaponItem.damage;  // Damage sesuai senjata
                        float knockBack = weaponItem.knockBack; // KnockBack sesuai senjata

                        // Pastikan nilai damage dan knockBack memiliki nilai minimal
                        if (damage <= 0)
                            damage = 10;
                        if (knockBack <= 0)
                            knockBack = 2f;

                        // Buat projectile tambahan dengan properti dari senjata di slot ini.
                        int projID = Projectile.NewProjectile(null, pos.X, pos.Y, velocityX, velocityY, projType, damage, knockBack, tsPlayer.Index);

                        // Kirim data projectile ke seluruh pemain agar sinkron.
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, NetworkText.FromLiteral(""), projID, 0f, 0f, 0f, 0);
                    }
                }
            }
        }
    }
}
