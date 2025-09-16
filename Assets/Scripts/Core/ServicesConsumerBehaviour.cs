using UnityEngine;

namespace MergeDungeon.Core
{
    /// <summary>
    /// Base MonoBehaviour that wires up to a GameplayServicesChannelSO and exposes
    /// the resolved GameplayServicesContext via the protected 'services' field.
    /// </summary>
    public class ServicesConsumerBehaviour : MonoBehaviour
    {
        [Header("Services")]
        public GameplayServicesChannelSO servicesChannel;

        protected GameplayServicesContext services { get; private set; }

        protected virtual void OnEnable()
        {
            if (servicesChannel != null)
            {
                servicesChannel.ServicesRegistered += OnServicesRegistered;
                servicesChannel.ServicesUnregistered += OnServicesUnregistered;
                if (servicesChannel.HasServices)
                {
                    services = servicesChannel.Current;
                    OnServicesReady();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (servicesChannel != null)
            {
                servicesChannel.ServicesRegistered -= OnServicesRegistered;
                servicesChannel.ServicesUnregistered -= OnServicesUnregistered;
            }
            if (services != null)
            {
                OnServicesLost();
                services = null;
            }
        }

        private void OnServicesRegistered(GameplayServicesContext ctx)
        {
            services = ctx;
            OnServicesReady();
        }

        private void OnServicesUnregistered(GameplayServicesContext ctx)
        {
            if (services == ctx)
            {
                OnServicesLost();
                services = null;
            }
        }

        protected virtual void OnServicesReady() {}
        protected virtual void OnServicesLost() {}
    }
}

