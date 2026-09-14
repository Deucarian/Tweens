using Deucarian.Common;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Tweens.Tests
{
    public sealed class TransformVisibilityPivotTests
    {
        private GameObject parent;
        private GameObject root;
        private Mesh mesh;
        private TweenScheduler scheduler;
        private TransformVisibilityBinding binding;
        private Vector3 localCenter;
        private Vector3 center;
        private Vector3 position;
        private Vector3 scale;

        [SetUp]
        public void Setup()
        {
            parent = new GameObject("Rotated nonuniform parent");
            parent.transform.SetPositionAndRotation(new Vector3(40, 7, -20), Quaternion.Euler(15, 35, 25));
            parent.transform.localScale = new Vector3(2, 3, 0.5f);
            root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.transform.SetParent(parent.transform, false);
            root.transform.localPosition = position = new Vector3(7, 2, 5);
            root.transform.localScale = scale = new Vector3(-2, 1.5f, 3);
            root.transform.localRotation = Quaternion.Euler(10, 20, 30);
            var filter = root.GetComponent<MeshFilter>();
            mesh = Object.Instantiate(filter.sharedMesh);
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] += new Vector3(100, 30, -70);
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            localCenter = mesh.bounds.center;
            center = root.transform.TransformPoint(localCenter);
            scheduler = new TweenScheduler();
            var settings = VisibilityTweenSettings.Enter;
            settings.seconds = 1;
            settings.easing = DeucarianEasing.Linear;
            binding = new TransformVisibilityBinding(scheduler, root, root.transform, settings, settings);
        }

        [TearDown]
        public void Teardown()
        {
            binding?.Dispose();
            scheduler?.Dispose();
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void OffOriginMeshKeepsItsVisibleCenterDuringHideAndReversal()
        {
            binding.SetVisible(false);
            scheduler.Advance(0.5f, 0.5f);
            Assert.That(Vector3.Distance(center, root.transform.TransformPoint(localCenter)), Is.LessThan(0.001f));
            Assert.That(root.transform.localScale, Is.EqualTo(scale * 0.5f));
            binding.SetVisible(true);
            scheduler.Advance(0.2f, 0.2f);
            Assert.That(Vector3.Distance(center, root.transform.TransformPoint(localCenter)), Is.LessThan(0.001f));
            scheduler.Advance(1, 1);
            Assert.That(root.transform.localPosition, Is.EqualTo(position));
            Assert.That(root.transform.localScale, Is.EqualTo(scale));
        }

        [Test]
        public void CancellationRestoresTheAuthoredPoseExactly()
        {
            binding.SetVisible(false);
            scheduler.Advance(0.4f, 0.4f);
            binding.RestoreImmediate(true);
            Assert.That(root.transform.localPosition, Is.EqualTo(position));
            Assert.That(root.transform.localScale, Is.EqualTo(scale));
            Assert.That(scheduler.ActiveCount, Is.Zero);
        }

        [Test]
        public void ExplicitTransformOriginPreservesAnAuthoredPivot()
        {
            var settings = binding.Enter;
            binding.Dispose();
            binding = new TransformVisibilityBinding(scheduler, root, root.transform, settings, settings,
                VisibilityScaleOrigin.TransformOrigin);
            binding.SetVisible(false);
            scheduler.Advance(0.5f, 0.5f);
            Assert.That(root.transform.localPosition, Is.EqualTo(position));
            Assert.That(root.transform.localScale, Is.EqualTo(scale * 0.5f));
        }

        [Test]
        public void InitiallyInactiveNestedMeshUsesItsBoundsBeforeShowing()
        {
            var settings = binding.Enter;
            binding.Dispose();
            Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            var child = new GameObject("Nested imported mesh");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(2, 8, 4);
            child.transform.localRotation = Quaternion.Euler(25, 50, 10);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>();
            center = child.transform.TransformPoint(mesh.bounds.center);
            localCenter = root.transform.InverseTransformPoint(center);
            root.SetActive(false);
            binding = new TransformVisibilityBinding(scheduler, root, root.transform, settings, settings);
            binding.SetVisible(true);
            Assert.That(Vector3.Distance(center, root.transform.TransformPoint(localCenter)), Is.LessThan(0.001f));
            scheduler.Advance(0.5f, 0.5f);
            Assert.That(Vector3.Distance(center, root.transform.TransformPoint(localCenter)), Is.LessThan(0.001f));
            Assert.That(mesh.bounds.center, Is.EqualTo(new Vector3(100, 30, -70)));
        }

        [Test]
        public void PoolReleaseReceivesTheExactBaselineAfterAnOffOriginExit()
        {
            bool returned = false;
            binding.HideAndRelease(value => {
                Assert.That(value.activeSelf, Is.False);
                Assert.That(value.transform.localPosition, Is.EqualTo(position));
                Assert.That(value.transform.localScale, Is.EqualTo(scale));
                returned = true;
            });
            scheduler.Advance(0.5f, 0.5f);
            scheduler.Advance(0.5f, 0.5f);
            Assert.That(returned, Is.True);
            Assert.That(scheduler.FaultCount, Is.Zero);
        }
    }
}
