using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TootTallyCustomCursor
{
    public class CursorTrail : MonoBehaviour
    {
        private Queue<float> _verticesTimes;
        private List<Vector3> _verticesList;
        private LineRenderer _lineRenderer;
        private float _lifetime;
        private float _trailSpeed;
        private int _refreshRate;
        private Vector3 _velocity;

        public void Awake()
        {
            if (!gameObject.TryGetComponent(out _lineRenderer))
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _verticesTimes = new Queue<float>();
            _verticesList = new List<Vector3>() { gameObject.transform.position };
            _velocity = Vector3.zero;
            AddPoint(0);
        }

        public void Init(float startWidth, float lifetime, float trailSpeed, Color startColor, Color endColor, Material material, int renderQueue, int refreshRate, Texture2D texture = null)
        {
            _lineRenderer.sortingOrder = 51;
            _lineRenderer.numCapVertices = 20;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.textureMode = LineTextureMode.DistributePerSegment;
            _lineRenderer.endWidth = 0;
            _lineRenderer.startWidth = startWidth;
            if (material != null)
                _lineRenderer.material = GameObject.Instantiate(material);
            _lineRenderer.material.renderQueue = renderQueue;
            if (texture != null)
                _lineRenderer.material.mainTexture = texture;

            _lineRenderer.colorGradient = new Gradient()
            {
                mode = GradientMode.Blend,
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                }
            };
            _lifetime = lifetime;
            _trailSpeed = trailSpeed;
            _refreshRate = refreshRate;
        }

        public void Update()
        {
            var time = Time.time;

            while (_verticesTimes.Count > 2 && _verticesTimes.Peek() + _lifetime < time)
                RemovePoint();

            var distance = (int)((_verticesList.First().y - gameObject.transform.position.y) * 1000);

            if (distance != 0)
                AddPoint(time);
            else if (_refreshRate == 0 || (time - _verticesTimes.Last() > 1f / _refreshRate && distance == 0))
                AddPoint(time);

            _velocity.x = -Time.deltaTime * _trailSpeed;
            for (int i = 1; i < _verticesList.Count; i++)
                _verticesList[i] += _velocity;
            SetFirstVerticePosition();

            _lineRenderer.positionCount = _verticesList.Count;
            _lineRenderer.SetPositions(_verticesList.ToArray());
        }

        public void SetWidth(float width)
        {
            _lineRenderer.startWidth = width;
        }

        public void UpdateColors()
        {
            _lineRenderer.colorGradient = new Gradient()
            {
                mode = GradientMode.Blend,
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Plugin.Instance.TrailStartColor.Value, 0f),
                    new GradientColorKey(Plugin.Instance.TrailEndColor.Value, 1f)
                }
            };
        }

        public void SetColors(GradientColorKey[] colors)
        {
            _lineRenderer.colorGradient = new Gradient()
            {
                mode = GradientMode.Blend,
                colorKeys = colors
            };
        }

        public void SetLifetime(float lifetime)
        {
            _lifetime = lifetime;
        }

        public void SetTrailSpeed(float trailSpeed)
        {
            _trailSpeed = trailSpeed;
        }

        public void SetRefreshRate(int refreshRate)
        {
            _refreshRate = refreshRate;
        }

        private void AddPoint(float time)
        {
            _verticesTimes.Enqueue(time);
            _verticesList.Insert(1, gameObject.transform.position);
        }

        private void SetFirstVerticePosition()
        {
            _verticesList[0] = gameObject.transform.position;
        }

        private void RemovePoint()
        {
            _verticesTimes.Dequeue();
            _verticesList.RemoveAt(_verticesList.Count - 1);
        }

        public void Dispose()
        {
            GameObject.DestroyImmediate(_lineRenderer);
            GameObject.DestroyImmediate(this);
        }
    }
}
