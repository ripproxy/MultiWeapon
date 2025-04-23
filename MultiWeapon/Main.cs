using System;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;

[ApiVersion(2, 1)]
public class MultiWeapon : TerrariaPlugin
{
    public override string Name => "MultiWeapon";
    public override string Author => "YourName";
    public override string Description => "Memungkinkan serangan bersamaan dari slot 0, 1, dan 2.";
    public override Version Version => new Version(1, 0, 0);

    public MultiWeapon(Main game) : base(game)
    {
    }

    public override void Initialize()
    {
        // Hook ke event di sini
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
        }
        base.Dispose(disposing);
    }
    private void OnGameUpdate(EventArgs args)
    {
    foreach (TSPlayer player in TShock.Players)
    {
        if (player == null || !player.Active || !player.TPlayer.controlUseItem)
            continue;

        // Periksa apakah pemain sedang menggunakan item dari slot 0
        Player tPlayer = player.TPlayer;
        Item selectedItem = tPlayer.inventory[tPlayer.selectedItem];

        if (tPlayer.selectedItem == 0 && selectedItem.damage > 0)
        {
            // Simulasikan penggunaan item dari slot 1 dan 2
            for (int i = 1; i <= 2; i++)
            {
                Item weapon = tPlayer.inventory[i];
                if (weapon != null && weapon.damage > 0)
                {
                    // Gunakan item dengan arah yang sama
                    player.SendData(PacketTypes.PlayerSlot, "", player.Index, i);
                    if (weapon.melee)
                    {
                        // Logika untuk melee (misalnya, animasi serangan)
                        tPlayer.itemAnimation = weapon.useAnimation;
                        NetMessage.SendData((int)PacketTypes.PlayerAnimation, -1, -1, null, player.Index);
                    }
                    else if (weapon.shoot > 0)
                    {
                        // Logika untuk projectile
                        int proj = Projectile.NewProjectile(
                            tPlayer.position,
                            tPlayer.direction * weapon.shootSpeed * new Microsoft.Xna.Framework.Vector2(1, 0),
                            weapon.shoot,
                            weapon.damage,
                            weapon.knockBack,
                            player.Index
                        );
                    }
                }
            }
        }
    }
}
    
