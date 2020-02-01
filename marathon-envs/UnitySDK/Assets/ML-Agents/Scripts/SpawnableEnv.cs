using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAgents
{
    public class SpawnableEnv: MonoBehaviour
    {
        [Space()]
        [Tooltip("How much padding bettween spawned environments as a multiple of the envionment size (i.e. 1 = a gap of one envionment.")]
        public float paddingBetweenEnvs;
        [Space()]
        public Bounds bounds;
        [Space()]
        [Tooltip("Creates a unique scene and physics scene for this envionment")]
        public bool CreateUniquePhysicsScene;

        Scene _spawnedScene;
        PhysicsScene _spawnedPhysicsScene;

        public void UpdateBounds()
        {
            bounds.size = Vector3.zero; // reset
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                bounds.Encapsulate(col.bounds);
            }
            TerrainCollider[] terrainColliders = GetComponentsInChildren<TerrainCollider>();
            foreach (TerrainCollider col in terrainColliders)
            {
                var b = new Bounds();
                b.center = col.transform.position + (col.terrainData.size/2);
                b.size =  col.terrainData.size;
                bounds.Encapsulate(b);
            }
        }
        public bool IsPointWithinBoundsInWorldSpace(Vector3 point)
        {
            var boundsInWorldSpace = new Bounds(
                bounds.center + transform.position,
                bounds.size
            );
            bool isInBounds = boundsInWorldSpace.Contains(point);
            return isInBounds;
        }

        public void SetSceneAndPhysicsScene(Scene spawnedScene, PhysicsScene spawnedPhysicsScene)
        {
            _spawnedScene = spawnedScene;
            _spawnedPhysicsScene = spawnedPhysicsScene;
        }
        public PhysicsScene GetPhysicsScene()
        {
            return _spawnedPhysicsScene != null ? _spawnedPhysicsScene : Physics.defaultPhysicsScene;
        }
        public static void TriggerPhysicsStep()
        {
            var uniquePhysicsEnvs = FindObjectsOfType<SpawnableEnv>()
                .Where(x=>x.CreateUniquePhysicsScene)
                .ToList();
            foreach (var env in uniquePhysicsEnvs)
            {
                env._spawnedPhysicsScene.Simulate(Time.fixedDeltaTime);
            }
        }
    }
}