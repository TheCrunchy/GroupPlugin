using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using CrunchGroup.Handlers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.EntityComponents.Blocks;
using Torch.Managers.PatchManager;
using VRageMath;

namespace Groups_private_Scripts.Sargonass
{
	// Utilizing Torch's PatchShim attribute to mark this class for patching
	[PatchShim]
	public class SargSafezone
	{
		// Static dictionary to store planetary bounding spheres by name
		public static Dictionary<string, BoundingSphereD> PlanetsToCheck = new Dictionary<string, BoundingSphereD>()
		{
			{ "UFP", new BoundingSphereD(new Vector3D(2000000, 0, 0), 500000)},
			{ "RSE",  new BoundingSphereD(new Vector3D(-6949747.4683058318, 4949747.4683058327, 0), 500000)},
			{ "OFA",  new BoundingSphereD(new Vector3D(-5949747.4683058346, -6681798.2758747088, 0), 500000)},
			{ "KLGN",  new BoundingSphereD(new Vector3D(6547018.4884004183, 6153377.5146099282, 0), 500000) },
			{ "CIU",  new BoundingSphereD(new Vector3D(6681798.2758747088, -5949747.4683058346, 0), 500000) }
		};

		// Retrieves a MethodInfo object for MySafeZoneComponent's private method using reflection
		internal static readonly MethodInfo startCountDownMethod =
			typeof(MySafeZoneComponent).GetMethod("StartActivationCountdown", BindingFlags.Instance | BindingFlags.NonPublic) ??
			throw new Exception("Failed to find patch method");

		// Retrieves a MethodInfo for the current class's public static method to use as a patch
		internal static readonly MethodInfo startCountDownMethodPatch =
			typeof(SargSafezone).GetMethod(nameof(SafezoneBlockPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
			throw new Exception("Failed to find patch method");

		// Method to apply the patch using Torch API
		public static void Patch(PatchContext ctx)
		{
			ctx.GetPattern(startCountDownMethod).Prefixes.Add(startCountDownMethodPatch);
			// Optional: Attach additional methods to the game's update cycle if needed
			// Core.UpdateCycle += UpdateExample;
		}

		// Variable to keep track of game ticks
		private static int ticks;
		// Scheduler for checks, set to run checks every 5 minutes from now
		public static DateTime NextSZCheck = DateTime.Now.AddMinutes(1);

		// Method to update periodically based on game ticks
		private static void UpdateExample()
		{
			ticks++; // Increment tick count
					 // Perform checks every 128 ticks and if it's time for the next scheduled check
			if (ticks % 128 == 0)
			{
				if (DateTime.Now > NextSZCheck)
				{
					DoChecks(); // Run the checks
				}
			}
		}

		
		// Patch method to control the activation of safezones based on faction and location
		public static bool SafezoneBlockPatchMethod(MySafeZoneComponent __instance)
		{
			MySafeZoneBlock SZ = __instance.Entity as MySafeZoneBlock; // Get the SafeZone block component
			MyFaction fac = MySession.Static.Factions.TryGetFactionByTag(SZ.GetOwnerFactionTag()); // Try to get the owner's faction
			if (fac == null) // If there's no such faction, deny activation
			{
				return false;
			}

			var group = GroupHandler.GetFactionsGroup(fac.FactionId); // Get the group for this faction

			if (group == null) // If no group is found, deny activation
			{
				return false;
			}

			// Check if the group's tag matches any predefined planetary bounding spheres
			if (PlanetsToCheck.TryGetValue(group.GroupTag, out var sphere))
			{
				// Check if the safezone's position is within the sphere
				var position = SZ.CubeGrid.PositionComp.GetPosition();
				if (sphere.Contains(position) == ContainmentType.Contains || sphere.Contains(position) == ContainmentType.Intersects)
				{
					return true; // Allow activation
				}

				return false; // Otherwise, deny activation
			}

			return false; // Deny activation if no matching tag is found
		}


		// Placeholder for additional checks or operations
		private static void DoChecks()
		{
			var groups = GroupHandler.LoadedGroups.Select(x => x.Value); // Example operation
		}
	}
}