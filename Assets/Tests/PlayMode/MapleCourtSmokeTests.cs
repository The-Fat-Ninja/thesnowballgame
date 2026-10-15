using System.Collections;
using NUnit.Framework;
using SnowballGame.Core;
using SnowballGame.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SnowballGame.Tests
{
    /// <summary>
    /// Loads the prototype scene and plays real turns through the controller.
    /// Any error or exception logged during play fails the test.
    /// </summary>
    public class MapleCourtSmokeTests
    {
        [UnityTest]
        public IEnumerator TurnsPlayOutInTheScene()
        {
            SceneManager.LoadScene("MapleCourt");
            yield return null;
            yield return null;

            var ctrl = Object.FindAnyObjectByType<MatchController>();
            Assert.IsNotNull(ctrl, "MatchController missing from scene");
            Assert.IsNotNull(ctrl.Match, "match not started");
            Assert.IsTrue(ctrl.IsPlanning);
            Assert.Greater(GameObject.Find("World").transform.childCount, 50, "world was not built");

            for (int turn = 1; turn <= 3 && !ctrl.IsOver; turn++)
            {
                // Walk the first home kid towards the middle and lob at the nearest enemy.
                Kid me = null;
                foreach (var k in ctrl.Match.Crew(Match.Home))
                    if (!k.Soaked) { me = k; break; }
                Assert.IsNotNull(me);
                var order = new KidOrder(me.Id);
                var step = new Vec2(me.Pos.X + 300, me.Pos.Z);
                if (ctrl.Match.IsPathClear(me.Pos, step)) order.Path.Add(step);
                foreach (var e in ctrl.Match.Crew(Match.Away))
                    if (!e.Soaked)
                    {
                        order.HasThrow = true;
                        order.Style = ThrowStyle.Lob;
                        order.ThrowTarget = e.Pos;
                        break;
                    }
                ctrl.SetOrder(order);

                int before = ctrl.Match.Turn;
                ctrl.Go();
                Assert.IsFalse(ctrl.IsPlanning, "GO! did not start playback");
                float t = 0;
                while (!ctrl.IsPlanning && !ctrl.IsOver && t < 6f)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                Assert.AreEqual(before + 1, ctrl.Match.Turn);
                Assert.IsTrue(ctrl.IsPlanning || ctrl.IsOver, "playback never finished");
            }
        }
    }
}
