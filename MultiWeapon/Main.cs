using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TshockAPI.Hooks;
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
        PlayerHooks.PlayerItemAnimation += OnItemAnimation;
    }

    private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        // Simpan posisi mouse saat attack
        if (args.Control.IsUsingItem)
        {
            attackDirections[args.PlayerId] = new Vector2(args.MouseX, args.MouseY);
        }
    }

    private void OnItemAnimation(object sender, PlayerItemAnimationEventArgs args)
    {
        if (args.ItemAnimationType != 1 || !attackDirections.ContainsKey(args.Player.Index))
            return;

        var player = args.Player;
        var mainSlot = player.TPlayer.selectedItem;
        var mousePos = attackDirections[args.Player.Index];

        if (mainSlot < 0 || mainSlot > 2)
            return;

        for (int slot = 0; slot < 3; slot++)
        {
            if (slot == mainSlot) continue;
            
            Item weapon = player.TPlayer.inventory[slot];
            if (IsValidWeapon(weapon))
            {
                // Simulasi dengan arah serangan sama
                SyncAttackDirection(player, slot, weapon, mousePos);
            }
        }
    }

    private void SyncAttackDirection(TSPlayer player, int slot, Item weapon, Vector2 mousePos)
    {
        // Konversi koordinat mouse relatif ke world position
        Vector2 worldMouse = new Vector2(
            player.TPlayer.position.X + mousePos.X - Main.screenWidth/2,
            player.TPlayer.position.Y + mousePos.Y - Main.screenHeight/2
        );

        // Simpan state asli
        Vector2 originalMouse = new Vector2(player.TPlayer.position.X, player.TPlayer.position.Y);
        int originalSlot = player.TPlayer.selectedItem;

        try
        {
            // Override posisi mouse dan slot
            player.TPlayer.selectedItem = slot;
            player.TPlayer.controlUseItem = true;
            
            // Proses arah serangan
            player.TPlayer.direction = (worldMouse.X > player.TPlayer.Center.X) ? 1 : -1;
            weapon.UseItem(player.Index);
            
            // Proyektil khusus
            if (weapon.shoot > 0)
            {
                Vector2 velocity = Vector2.Normalize(worldMouse - player.TPlayer.Center) * weapon.shootSpeed;
                Projectile.NewProjectile(
                    player.TPlayer.GetProjectileSource_Item(weapon),
                    player.TPlayer.Center,
                    velocity,
                    weapon.shoot,
                    weapon.damage,
                    weapon.knockBack,
                    player.Index
                );
            }

            // Update animasi
            NetMessage.SendData((int)PacketTypes.PlayerItemAnimation, -1, -1, 
                NetworkText.Empty, player.Index, slot, 1);
        }
        finally
        {
            // Reset state
            player.TPlayer.selectedItem = originalSlot;
        }
    }

    private bool IsValidWeapon(Item item)
    {
        return item.active && item.damage > 0 && item.pick == 0 && 
               item.axe == 0 && item.hammer == 0 && !item.notAmmo;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GetDataHandlers.PlayerUpdate.UnRegister(this, OnPlayerUpdate);
            PlayerHooks.PlayerItemAnimation -= OnItemAnimation
        }
        base.Dispose(disposing);
    }
}
