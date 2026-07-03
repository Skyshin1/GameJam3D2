using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed class AnchorDefensePlayModeTests
    {
        [UnityTest]
        public IEnumerator ExistingThreeDimensionalVersionBootsAndSpawnsEnemies()
        {
            yield return VerifyPlayableLoop("Gameplay", false);
        }

        [UnityTest]
        public IEnumerator DirectionalSpriteVersionBootsAndSpawnsEnemies()
        {
            yield return VerifyPlayableLoop("Gameplay_DirectionalSprites", true);
        }

        private static IEnumerator VerifyPlayableLoop(string sceneName, bool expectDirectionalSprites)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
            Assert.That(loadOperation, Is.Not.Null);
            while (!loadOperation.isDone)
            {
                yield return null;
            }
            yield return null;

            Assert.That(Object.FindObjectOfType<GameBootstrap>(), Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<OrbitRingController>().Length, Is.EqualTo(3));
            Assert.That(Object.FindObjectsOfType<TurretController>().Length, Is.EqualTo(18));
            Assert.That(Object.FindObjectOfType<CoreHealth>(), Is.Not.Null);
            Assert.That(Object.FindObjectOfType<HudController>(), Is.Not.Null);

            RingInputController inputController = Object.FindObjectOfType<RingInputController>();
            Assert.That(inputController, Is.Not.Null);
            Camera gameplayCamera = Camera.main;
            Vector3 fixedCameraPosition = gameplayCamera.transform.position;
            inputController.SetCameraOrbitMode(CameraOrbitMode.XYPlane);
            inputController.ApplyCameraOrbitDelta(new Vector2(30f, 0f));
            Assert.That(Vector3.Distance(gameplayCamera.transform.position, fixedCameraPosition), Is.GreaterThan(0.01f));
            Vector3 planarCameraPosition = gameplayCamera.transform.position;
            inputController.SetCameraOrbitMode(CameraOrbitMode.FreeOrbit360);
            inputController.ApplyCameraOrbitDelta(new Vector2(20f, 15f));
            Assert.That(Vector3.Distance(gameplayCamera.transform.position, planarCameraPosition), Is.GreaterThan(0.01f));
            inputController.SetCameraOrbitMode(CameraOrbitMode.Disabled);
            Assert.That(Vector3.Distance(gameplayCamera.transform.position, fixedCameraPosition), Is.LessThan(0.001f));

            int directionalVisualCount = Object.FindObjectsOfType<DirectionalSpriteRenderer>().Length;
            if (expectDirectionalSprites)
            {
                Assert.That(directionalVisualCount, Is.GreaterThanOrEqualTo(18));
            }
            else
            {
                Assert.That(directionalVisualCount, Is.EqualTo(0));
            }

            EndlessEnemySpawner spawner = Object.FindObjectOfType<EndlessEnemySpawner>();
            Assert.That(spawner, Is.Not.Null);
            float timeout = Time.realtimeSinceStartup + 5f;
            while (spawner.TotalSpawned == 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            Assert.That(spawner.TotalSpawned, Is.GreaterThan(0));
        }
    }
}
