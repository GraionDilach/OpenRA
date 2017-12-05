﻿using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can recieve attachments through AttachToTargetWarheads.")]
	public class DelayedWeaponAttachableInfo : ITraitInfo
	{
		[Desc("Types of actors that it can attach to, as long as the type also exists in the Attachable Type: trait.")]
		public readonly HashSet<string> AttachableTypes = new HashSet<string> { "bomb" };

		[Desc("Defines how many objects can be attached at any given time.")]
		public readonly int AttachLimit = 1;
		
		public object Create(ActorInitializer init) { return new DelayedWeaponAttachable(this); }
	}

	public class DelayedWeaponAttachable : ITick
	{
		public readonly DelayedWeaponAttachableInfo Info;

		public DelayedWeaponAttachable(DelayedWeaponAttachableInfo info) { Info = info; }

		private HashSet<DelayedWeaponTrigger> container = new HashSet<DelayedWeaponTrigger>();
		
		public void Tick(Actor self)
		{
			foreach (var bomb in container)
			{
				bomb.Tick(self);
			}

			container.RemoveWhere(p => !p.IsValid);
		}

		public bool CanAttach(string type)
		{
			return Info.AttachableTypes.Contains(type) && container.Count < Info.AttachLimit;
		}

		public void Attach(DelayedWeaponTrigger bomb)
		{
			container.Add(bomb);
		}
	}
}
