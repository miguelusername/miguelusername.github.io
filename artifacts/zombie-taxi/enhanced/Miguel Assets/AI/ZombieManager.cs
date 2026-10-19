using System.Collections.Generic;
using UnityEngine;
using ZombieTaxi.Core;

/// <summary>
/// Central owner of the state and scheduling shared by the whole horde.
///
/// Holds the one cached reference to the player, services a bounded slice of
/// the zombie population each frame so per-frame AI cost does not scale with
/// the population, and maintains a spatial index other systems query instead of
/// scanning the zombie list.
///
/// It creates itself on demand, so no scene setup is required.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ZombieManager : MonoBehaviour
{
    private static ZombieManager instance;

    [Header("Update Budget")]
    [Tooltip("Zombies serviced per frame. Per-frame AI cost is capped by this, not by the population.")]
    [SerializeField] private int aiUpdatesPerFrame = 25;

    [Header("Detail Bands")]
    [Tooltip("Within this distance a zombie chases at full rate.")]
    [SerializeField] private float nearBandDistance = 32f;
    [Tooltip("Beyond this distance a zombie stops doing expensive work.")]
    [SerializeField] private float midBandDistance = 90f;
    [Tooltip("Seconds between path recalculations for a near zombie. Scaled up for farther bands.")]
    [SerializeField] private float baseRepathInterval = 0.25f;
    [Tooltip("A path is only recalculated if the target has moved at least this far since last time. Keep this small: a large value makes chasers steer at a stale position and trail the taxi.")]
    [SerializeField] private float repathMoveThreshold = 0.75f;

    [Header("Spatial Index")]
    [Tooltip("Grid cell size. Works best near the radius of the most common query.")]
    [SerializeField] private float spatialCellSize = 25f;
    [Tooltip("Seconds between rebuilds of the spatial index.")]
    [SerializeField] private float spatialRebuildInterval = 0.2f;

    private readonly List<Zombie> zombies = new();
    private readonly UpdateBudgetScheduler scheduler = new();
    private SpatialHashGrid<Zombie> spatialIndex;
    private AiDetailBands detailBands;

    private Transform playerTransform;
    private float nextSpatialRebuildTime;

    /// <summary>True while the player is inside an anti-zombie zone.</summary>
    public bool PlayerInAntiZombieZone { get; private set; }

    public Transform PlayerTransform => playerTransform;
    public AiDetailBands DetailBands => detailBands;
    public float BaseRepathInterval => baseRepathInterval;
    public float RepathMoveThreshold => repathMoveThreshold;
    public int ZombieCount => zombies.Count;

    /// <summary>Ticks needed to service every zombie once. Handy for tuning and diagnostics.</summary>
    public int TicksPerFullPass => UpdateBudgetScheduler.TicksPerFullPass(zombies.Count, aiUpdatesPerFrame);

    /// <summary>
    /// Returns the manager, creating one if the scene does not contain it.
    /// Keeps the enhancement self-contained: no prefab or scene wiring needed.
    /// </summary>
    public static ZombieManager Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = FindFirstObjectByType<ZombieManager>();

            if (instance == null)
            {
                GameObject host = new("ZombieManager");
                instance = host.AddComponent<ZombieManager>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        spatialIndex = new SpatialHashGrid<Zombie>(spatialCellSize);
        detailBands = new AiDetailBands(nearBandDistance, midBandDistance);

        CachePlayer();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// The one scene search for the player. Everything else reads
    /// <see cref="PlayerTransform"/>.
    /// </summary>
    private void CachePlayer()
    {
        S_TaxiPlayer_Test player = FindFirstObjectByType<S_TaxiPlayer_Test>();
        if (player != null) playerTransform = player.transform;
    }

    public void Register(Zombie zombie)
    {
        if (zombie == null || zombies.Contains(zombie)) return;
        zombies.Add(zombie);
    }

    public void Unregister(Zombie zombie)
    {
        if (zombie == null) return;

        int index = zombies.IndexOf(zombie);
        if (index < 0) return;

        // Swap-remove: O(1) instead of shifting the tail of the list.
        // Order does not matter because the scheduler cycles the whole list anyway.
        int last = zombies.Count - 1;
        zombies[index] = zombies[last];
        zombies.RemoveAt(last);
    }

    /// <summary>Set by the anti-zombie zone; read by every zombie on its next tick.</summary>
    public void SetPlayerInAntiZombieZone(bool inZone)
    {
        PlayerInAntiZombieZone = inZone;
    }

    private void Update()
    {
        // The player can be spawned after this manager, so keep trying until found.
        if (playerTransform == null) CachePlayer();

        if (Time.time >= nextSpatialRebuildTime)
        {
            RebuildSpatialIndex();
            nextSpatialRebuildTime = Time.time + spatialRebuildInterval;
        }

        ServiceSlice();
    }

    /// <summary>
    /// O(n) rebuild, throttled by <see cref="spatialRebuildInterval"/>. Each
    /// entry is just a reference and a position, and one rebuild serves every
    /// query until the next.
    /// </summary>
    private void RebuildSpatialIndex()
    {
        spatialIndex.Clear();

        for (int i = zombies.Count - 1; i >= 0; i--)
        {
            Zombie zombie = zombies[i];

            // Destroyed zombies are pruned during this pass so no separate sweep is needed.
            if (zombie == null)
            {
                int last = zombies.Count - 1;
                zombies[i] = zombies[last];
                zombies.RemoveAt(last);
                continue;
            }

            spatialIndex.Insert(zombie, zombie.transform.position);
        }
    }

    /// <summary>Services the next budgeted slice of zombies, wrapping around the list.</summary>
    private void ServiceSlice()
    {
        int count = zombies.Count;
        if (!scheduler.TryGetSlice(count, aiUpdatesPerFrame, out int start, out int length)) return;

        for (int offset = 0; offset < length; offset++)
        {
            int index = UpdateBudgetScheduler.IndexAt(start, offset, count);

            Zombie zombie = zombies[index];
            if (zombie == null) continue;

            zombie.ManagedTick(playerTransform);
        }
    }

    /// <summary>Nearest zombie to a point, resolved through the spatial index.</summary>
    public bool TryFindNearestZombie(Vector3 position, float radius, out Zombie nearest)
    {
        if (spatialIndex == null)
        {
            nearest = null;
            return false;
        }

        return spatialIndex.TryFindNearest(position, radius, out nearest);
    }

    /// <summary>Appends every zombie within radius to the caller's list.</summary>
    public void QueryZombies(Vector3 position, float radius, List<Zombie> results)
    {
        spatialIndex?.Query(position, radius, results);
    }
}
