using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace MultiWeaponPlugin
{
    [ApiVersion(2, 1)]
    public class MultiWeapon : TerrariaPlugin
    {
        public override string Name => "MultiWeapon";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "ChatGPT";
        public override string Description => "Allows simultaneous attacks from weapon slots 0, 1, and 2 with synchronized direction and individual weapon properties.";

        public MultiWeapon(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
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
            // Loop through all players on the server
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player == null || !player.active || player.dead)
                    continue;

                // Only proceed if player is attacking (itemAnimation > 0)
                if (player.itemAnimation > 0)
                {
                    // We will synchronize the attack direction based on the first weapon's direction
                    // and then trigger attacks for slots 1 and 2 accordingly.

                    // Get the main weapon slot index 0
                    int mainSlot = 0;
                    int[] slotsToAttack = new int[] { 0, 1, 2 };

                    // Get the direction vector from the player's itemRotation and direction
                    // itemRotation is in radians, direction is 1 or -1
                    float direction = player.direction;
                    float rotation = player.itemRotation;

                    // Calculate the attack direction vector
                    Vector2 attackDir = new Vector2((float)Math.Cos(rotation) * direction, (float)Math.Sin(rotation) * direction);
                    attackDir.Normalize();

                    // We will only trigger additional attacks if the player is currently using the main slot weapon
                    // To avoid multiple triggers, we check if the player is currently using the weapon in slot 0
                    // This is a simplification; Terraria does not natively support multiple weapon attacks simultaneously,
                    // so we simulate by spawning projectiles and melee hitboxes for slots 1 and 2.

                    // Get the item in slot 0
                    var mainItem = player.inventory[mainSlot];
                    if (mainItem == null || mainItem.IsAir)
                        continue;

                    // We only run this logic once per itemAnimation cycle, when itemAnimation == itemAnimationMax - 1
                    // to avoid multiple triggers per swing
                    if (player.itemAnimation != player.itemAnimationMax - 1)
                        continue;

                    // For each slot 1 and 2, if the item is a weapon, simulate attack
                    for (int slotIndex = 1; slotIndex <= 2; slotIndex++)
                    {
                        var item = player.inventory[slotIndex];
                        if (item == null || item.IsAir)
                            continue;

                        if (!IsWeapon(item))
                            continue;

                        // Synchronize the attack direction to the main weapon's direction
                        // We simulate the attack by spawning projectiles or melee hitboxes server-side

                        // Melee weapons: spawn melee hitbox
                        if (IsMeleeWeapon(item))
                        {
                            SpawnMeleeHitbox(player, item, attackDir);
                        }
                        else if (IsRangedWeapon(item))
                        {
                            SpawnProjectile(player, item, attackDir);
                        }
                        else if (IsMagicWeapon(item))
                        {
                            SpawnProjectile(player, item, attackDir);
                        }
                        // Other weapon types can be added here if needed
                    }
                }
            }
        }

        private bool IsWeapon(Item item)
        {
            // Check if item is a weapon (melee, ranged, magic)
            return IsMeleeWeapon(item) || IsRangedWeapon(item) || IsMagicWeapon(item);
        }

        private bool IsMeleeWeapon(Item item)
        {
            return item.DamageType == DamageClass.Melee;
        }

        private bool IsRangedWeapon(Item item)
        {
            return item.DamageType == DamageClass.Ranged;
        }

        private bool IsMagicWeapon(Item item)
        {
            return item.DamageType == DamageClass.Magic;
        }

        private void SpawnMeleeHitbox(Player player, Item item, Vector2 direction)
        {
            // Spawn a melee hitbox in the direction of attack with the item's damage and knockback
            // Terraria server does not have direct API to spawn melee hitboxes,
            // so we simulate by spawning a short-lived projectile with melee properties

            int projType = GetMeleeProjectileType(item);
            if (projType == 0)
                return;

            Vector2 spawnPos = player.Center + direction * 40f; // 40 pixels in front of player

            int damage = item.damage;
            float knockBack = item.knockBack;

            int proj = Projectile.NewProjectile(player.GetSource_ItemUse(item), spawnPos, direction * 10f, projType, damage, knockBack, player.whoAmI);
            Main.projectile[proj].melee = true;
            Main.projectile[proj].timeLeft = 10; // short duration
        }

        private int GetMeleeProjectileType(Item item)
        {
            // Return a suitable melee projectile type for the item animation type
            // Use vanilla swing or thrust projectiles as proxy

            // Animation IDs:
            // 1 = swing
            // 3 = thrust

            // We can check item.useStyle for swing/thrust:
            // useStyle 1 = swinging
            // useStyle 3 = stabbing/thrusting

            if (item.useStyle == 1)
                return ProjectileID.MeleeSwing; // vanilla swing projectile
            else if (item.useStyle == 3)
                return ProjectileID.MeleeStab; // vanilla stab projectile

            return 0;
        }

        private void SpawnProjectile(Player player, Item item, Vector2 direction)
        {
            // Spawn the projectile for ranged or magic weapons with the item's properties

            if (item.shoot <= 0)
                return;

            Vector2 spawnPos = player.Center + direction * 20f;

            int damage = item.damage;
            float knockBack = item.knockBack;

            int proj = Projectile.NewProjectile(player.GetSource_ItemUse(item), spawnPos, direction * item.shootSpeed, item.shoot, damage, knockBack, player.whoAmI);

            // Set projectile type flags
            if (IsRangedWeapon(item))
                Main.projectile[proj].ranged = true;
            else if (IsMagicWeapon(item))
                Main.projectile[proj].magic = true;

            // For magic weapons like Nightglow (animation ID 14), no special handling needed here
        }
    }
}
