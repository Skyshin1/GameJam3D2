using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed class ControllerNavigationPlayModeTests
    {
        [UnityTest]
        public IEnumerator VirtualCursorStartsWithGamepadAndSemanticTarget()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            GameObject target = new GameObject("Cursor Snap Target");
            var context = new TestCursorContext(target);

            GamepadVirtualCursorController.SetContext(context);
            yield return null;

            GamepadVirtualCursorController cursor =
                Object.FindObjectOfType<GamepadVirtualCursorController>();
            Assert.That(cursor, Is.Not.Null);
            Assert.That(cursor.gameObject.activeInHierarchy, Is.True);

            GamepadVirtualCursorController.ClearContext(context);
            InputSystem.RemoveDevice(gamepad);
            Object.Destroy(target);
            Object.Destroy(cursor.gameObject);
            yield return null;
        }

        private sealed class TestCursorContext : IControllerCursorContext
        {
            private readonly Object owner;

            public TestCursorContext(Object targetOwner)
            {
                owner = targetOwner;
            }

            public bool IsControllerCursorActive => true;

            public void CollectControllerCursorTargets(List<ControllerCursorSnapTarget> targets)
            {
                targets.Add(new ControllerCursorSnapTarget(owner, new Vector2(640f, 360f), 100f));
            }
        }
    }
}
