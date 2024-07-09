using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Botany;
using Content.Shared.Construction.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Materials;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server.Botany.Components.BiomassGenerator;

public sealed class BiomassGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;


    [ValidatePrototypeId<MaterialPrototype>]
    public const string BiomassPrototype = "Biomass";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveBiomassGeneratorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActiveBiomassGeneratorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveBiomassGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<BiomassGeneratorComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<BiomassGeneratorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<BiomassGeneratorComponent, BiomassGeneratorDoAfterEvent>(OnDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveBiomassGeneratorComponent, BiomassGeneratorComponent>();
        while (query.MoveNext(out var uid, out var _, out var generator))
        {
            generator.ProcessingTimer -= frameTime;

            if (!_powerReceiverSystem.IsPowered(uid))
                continue;

            if (generator.ProcessingTimer > 0)
                continue;


            var actualYield = (int) (generator.CurrentExpectedYield);
            generator.CurrentExpectedYield -= actualYield;
            _material.TryChangeMaterialAmount(uid, generator.RequiredMaterial, actualYield);
            RemComp<ActiveBiomassGeneratorComponent>(uid);
        }
    }

    private void OnInit(EntityUid uid, ActiveBiomassGeneratorComponent component, ComponentInit args)
    {
        _sharedAudioSystem.PlayPvs("/Audio/Machines/reclaimer_startup.ogg", uid);
        _ambientSoundSystem.SetAmbience(uid, true);
    }

    private void OnShutdown(EntityUid uid, ActiveBiomassGeneratorComponent component, ComponentShutdown args)
    {
        _ambientSoundSystem.SetAmbience(uid, false);
    }

    private void OnPowerChanged(EntityUid uid, BiomassGeneratorComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (component.ProcessingTimer > 0)
                EnsureComp<ActiveBiomassGeneratorComponent>(uid);
        }
        else
            RemComp<ActiveBiomassGeneratorComponent>(uid);
    }

    private void OnUnanchorAttempt(EntityUid uid, ActiveBiomassGeneratorComponent component, UnanchorAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAfterInteractUsing(Entity<BiomassGeneratorComponent> gen, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!CanGrind(gen, args.Used))
            return;

        if (!TryComp<PhysicsComponent>(args.Used, out var physics))
            return;

        var delay = gen.Comp.BaseInsertionDelay * physics.FixturesMass;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            delay,
            new BiomassGeneratorDoAfterEvent(),
            gen,
            target: args.Target,
            used: args.Used)
        {
            NeedHand = true,
            BreakOnMove = true,
        });
    }

    private void OnDoAfter(Entity<BiomassGeneratorComponent> ent, ref BiomassGeneratorDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Used == null || args.Args.Target == null ||
            !HasComp<BiomassGeneratorComponent>(args.Args.Target.Value))
            return;

        StartProcessing(args.Args.Used.Value, ent);

        args.Handled = true;
    }

    private void StartProcessing(EntityUid toProcess, Entity<BiomassGeneratorComponent> ent, PhysicsComponent? physics = null)
    {
        if (!Resolve(toProcess, ref physics))
            return;

        var component = ent.Comp;
        EnsureComp<ActiveBiomassGeneratorComponent>(ent);

        var expectedYield = physics.FixturesMass * component.YieldPerUnitMass;
        component.CurrentExpectedYield += expectedYield;

        component.ProcessingTimer += physics.FixturesMass * component.ProcessingTimePerUnitMass;

        var inventory = _inventory.GetHandOrInventoryEntities(toProcess);
        foreach (var item in inventory)
        {
            _transform.DropNextTo(item, ent.Owner);
        }

        QueueDel(toProcess);
    }

    private bool CanGrind(Entity<BiomassGeneratorComponent> gen, EntityUid item)
    {
        //Right now just plants so just return hascomp's eval
        return HasComp<ProduceComponent>(item);
    }

    public void UpdateStatus(EntityUid genUid, BiomassGeneratorStatus status, BiomassGeneratorComponent generator)
    {
        generator.Status = status;
        _appearance.SetData(genUid, BiomassGeneratorVisuals.Status, generator.Status);
    }











}
