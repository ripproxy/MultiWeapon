using System; 
using Microsoft.Xna.Framework; 
using Terraria; 
using TerrariaApi.Server; 
using TShockAPI; 
using Terraria.ID;

namespace MultiWeaponPlugin { [ApiVersion(2, 1)] public class MultiWeaponPlugin : TerrariaPlugin { public override string Name => "MultiWeaponPlugin"; public override string Author => "ChatGPT"; public override string Description => "Slot 0, 1, dan 2 menyerang bersamaan tanpa delay dengan properti senjata masing-masing."; public override Version Version => new Version(1, 0, 0);

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
        if (args.Handled || args.MsgID != PacketTypes.PlayerAnimation)
            return;

        int playerIndex = args.Msg.whoAmI;
        TSPlayer tsPlayer = TShock.Players[playerIndex];
        if (tsPlayer == null || !tsPlayer.Active)
            return;

        Player player = tsPlayer.TPlayer;
        int animation = player.itemAnimation;
        int animID = player.bodyFrame.Y / 56;

        // Hanya respon pada animasi serangan tertentu
        if (!(animID == 1 || animID == 3 || animID == 5 || animID == 14))
            return;

        int selectedSlot = player.selectedItem;

        foreach (int slot in weaponSlots)
        {
            Item weapon = player.inventory[slot];
            if (weapon == null || weapon.damage <= 0)
                continue;

            if (weapon.shoot > 0)
            {
                Vector2 position = player.Center;
                float speed = weapon.shootSpeed > 0 ? weapon.shootSpeed : 10f;
                float rotation = player.itemRotation;
                if (player.direction == -1) rotation += (float)Math.PI;

                Vector2 velocity = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * speed;

                int projID = Projectile.NewProjectile(null, position.X, position.Y, velocity.X, velocity.Y,
                    weapon.shoot, weapon.damage, weapon.knockBack, playerIndex);

                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projID);
            }
            else
            {
                // Melee attack simulasi sederhana
                foreach (var npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && Vector2.Distance(player.Center, npc.Center) < weapon.width * 1.5f)
                    {
                        npc.StrikeNPC(weapon.damage, weapon.knockBack, player.direction);
                    }
                }
            }
        }
    }
}

}

