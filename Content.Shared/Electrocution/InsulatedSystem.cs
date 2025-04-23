// Transfer of closed PR of Wizards 31841
using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;

namespace Content.Shared.Electrocution;

public sealed partial class InsulatedSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InsulatedComponent, InventoryRelayedEvent<ShotAttemptedEvent>>(OnShoot);
    }
    private void OnShoot(EntityUid uid, InsulatedComponent comp, ref InventoryRelayedEvent<ShotAttemptedEvent> args)
    {
        if (comp.PreventOpperatinGuns && !args.Args.Used.Comp.BigTrigger)
        {
            PopupSystem.PopupClient(Loc.GetString("изолированные перчатки не позволяют это сделать"), args.Args.User); #Sunrise-edit
            args.Args.Cancel();
        }
    }
}