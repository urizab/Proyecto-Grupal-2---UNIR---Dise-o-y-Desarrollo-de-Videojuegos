using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de fuego simplificado que reduce su tamaño al recibir agua
/// y se destruye al apagarse por completo.
/// Incluye spawneo de fuegos buscadores y penalización al agua del jugador por contacto.
/// </summary>
public class FuegoExtinguible : MonoBehaviour
{
    [Header("── CONFIGURACIÓN BÁSICA")]
    [Tooltip("Intensidad o tamaño inicial del fuego.")]
    public float intensidadMaxima = 100f;
    
    [Tooltip("Intensidad actual del fuego.")]
    public float intensidadActual;

    [Header("── PENALIZACIÓN AL JUGADOR")]
    [Tooltip("Cantidad de agua que pierde el jugador por segundo al estar dentro de este fuego.")]
    public float penalizacionAguaAlJugador = 20f;

    [Header("── HABILIDAD: SPAWN DE FUEGOS BUSCADORES")]
    [Tooltip("¿Este fuego es capaz de spawnear pequeños fuegos que persiguen al jugador?")]
    public bool puedeSpawnearFuegosBuscadores = false;

    [Tooltip("Prefab del fuego pequeño buscador (ej. VFX_Fire 1).")]
    public GameObject prefabFuegoBuscador;

    [Tooltip("Rango de distancia al jugador para activar el spawneo.")]
    public float rangoActivacionSpawn = 8f;

    [Tooltip("Tiempo en segundos de espera entre spawns.")]
    public float intervaloSpawn = 4f;

    [Tooltip("Cantidad máxima de fuegos pequeños activos simultáneamente spawned por este fuego.")]
    public int cantidadMaximaSpawn = 3;

    [Tooltip("Velocidad de movimiento de los fuegos pequeños.")]
    public float velocidadFuegoBuscador = 3f;

    [Tooltip("Restricción del eje o plano de movimiento para los fuegos pequeños.")]
    public FuegoBuscador.RestriccionMovimiento restriccionMovimiento = FuegoBuscador.RestriccionMovimiento.SoloPlanoXZ;

    private Vector3 _escalaOriginal;
    private ParticleSystem[] _particulas;
    
    // Control de Spawneo
    private Transform _jugador;
    private float _temporizadorSpawn = 0f;
    private List<GameObject> _spawnsActivos = new List<GameObject>();
    private bool _estaExtinguido = false;

    private void Start()
    {
        intensidadActual = intensidadMaxima;
        _escalaOriginal = transform.localScale;

        // Detectar automáticamente sistemas de partículas en hijos
        _particulas = GetComponentsInChildren<ParticleSystem>();

        // Buscar al jugador automáticamente usando FirstPersonController
        var controladorJugador = FindFirstObjectByType<FirstPersonController>();
        if (controladorJugador != null)
        {
            _jugador = controladorJugador.transform;
        }
    }

    private void Update()
    {
        if (_estaExtinguido) return;

        // Lógica de Spawneo Dinámico si está habilitado y el jugador está cerca
        if (puedeSpawnearFuegosBuscadores && prefabFuegoBuscador != null && _jugador != null)
        {
            // Limpiar de la lista los fuegos que ya se hayan destruido o disipado
            _spawnsActivos.RemoveAll(item => item == null);

            // Medir distancia al jugador
            float distanciaAlJugador = Vector3.Distance(transform.position, _jugador.position);

            if (distanciaAlJugador <= rangoActivacionSpawn)
            {
                _temporizadorSpawn += Time.deltaTime;

                if (_temporizadorSpawn >= intervaloSpawn)
                {
                    _temporizadorSpawn = 0f;

                    // Comprobar si aún no hemos alcanzado el límite permitido de fuegos activos
                    if (_spawnsActivos.Count < cantidadMaximaSpawn)
                    {
                        SpawnearFuegoBuscador();
                    }
                }
            }
            else
            {
                // Pausar o resetear el temporizador gradualmente si el jugador sale del rango
                _temporizadorSpawn = Mathf.Max(0f, _temporizadorSpawn - Time.deltaTime);
            }
        }
    }


    /// Spawnea una pequeña llama y le añade y configura el comportamiento buscador.

    private void SpawnearFuegoBuscador()
    {
        GameObject instanciado = Instantiate(prefabFuegoBuscador, transform.position, Quaternion.identity);
        instanciado.transform.localScale = Vector3.one * 0.4f;

        FuegoBuscador scriptBuscador = instanciado.GetComponent<FuegoBuscador>();
        if (scriptBuscador == null)
        {
            scriptBuscador = instanciado.AddComponent<FuegoBuscador>();
        }

        scriptBuscador.objetivo = _jugador;
        scriptBuscador.velocidad = velocidadFuegoBuscador;
        scriptBuscador.restriccionMovimiento = restriccionMovimiento;

        _spawnsActivos.Add(instanciado);
    }


    /// Recibe una cantidad de agua que apaga y encoge el fuego progresivamente.

    public void RecibirAgua(float cantidad)
    {
        if (_estaExtinguido) return;

        intensidadActual -= cantidad;

        // Si la intensidad llega a 0 o menos, el fuego se apaga del todo
        if (intensidadActual <= 0f)
        {
            intensidadActual = 0f;
            ApagarFuego();
        }
        else
        {
            ActualizarFuego();
        }
    }


    /// Actualiza la escala del objeto y la emisión de partículas.

    private void ActualizarFuego()
    {
        float ratio = intensidadActual / intensidadMaxima;

        transform.localScale = _escalaOriginal * ratio;

        if (_particulas != null)
        {
            foreach (var ps in _particulas)
            {
                if (ps == null) continue;
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = ratio;
            }
        }
    }


    /// Apaga el fuego deteniendo las partículas y destruyendo el objeto.

    private void ApagarFuego()
    {
        _estaExtinguido = true;
        transform.localScale = Vector3.zero;

        if (_particulas != null)
        {
            foreach (var ps in _particulas)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        foreach (var instanciado in _spawnsActivos)
        {
            if (instanciado != null)
            {
                var childParticles = instanciado.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in childParticles)
                {
                    if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                Destroy(instanciado, 0.5f);
            }
        }
        _spawnsActivos.Clear();

        Destroy(gameObject, 2f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_estaExtinguido) return;

        // Si el jugador permanece dentro del fuego, drenar sus reservas progresivamente
        if (other.TryGetComponent<DisparadorAgua>(out var disparador))
        {
            disparador.RestarAgua(penalizacionAguaAlJugador * Time.deltaTime);
        }
    }
}
