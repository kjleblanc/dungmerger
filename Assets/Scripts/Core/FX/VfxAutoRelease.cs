using UnityEngine;

namespace MergeDungeon.Core
{
    // Attach to ParticleSystem-based VFX to auto-return to pool (or destroy) when finished.
    public class VfxAutoRelease : MonoBehaviour
    {
        private ParticleSystem _ps;
        private VfxManager _mgr;
        private VfxPoolKey _key;

        private void Awake()
        {
            _ps = GetComponentInChildren<ParticleSystem>();
            _key = GetComponent<VfxPoolKey>();
            _mgr = _key != null ? _key.manager : GetComponentInParent<VfxManager>();
        }

        private void OnEnable()
        {
            if (_ps != null && !_ps.isPlaying)
                _ps.Play(true);
        }

        // Called by Unity when all ParticleSystems on this GameObject and its children are stopped
        private void OnParticleSystemStopped()
        {
            if (_mgr != null)
            {
                _mgr.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

