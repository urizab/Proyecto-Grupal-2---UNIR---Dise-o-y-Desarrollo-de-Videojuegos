using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fuego de tipo Spawner. Hereda de Fuego.
/// Permanece estático en la escena y tiene la habilidad de invocar pequeños fuegos buscadores
/// que persiguen activamente al jugador si entra en cierto rango.
/// </summary>
public class FuegoSpawner : Fuego
{
    [Header("── CONFIGURACIÓN DE SPAWNER")]
    [Tooltip("¿Este fuego es capaz de spawnear pequeños fuegos que persiguen al jugador?")]
    public bool puedeSpawnearFuegosBuscadores = true;

    [Tooltip("Prefab del fuego pequeño buscador (ej. VFX_Fire 1).")]
    public GameObject prefabFuegoBuscador;

    [Tooltip("Rango de distancia al jugador para activar el spawneo.")]
    public float rangoActivacionSpawn = 8f;

    [Tooltip("Tiempo en segundos de espera entre cada invocación.")]
    public float intervaloSpawn = 4f;

    [Tooltip("Cantidad máxima de fuegos buscadores activos a la vez generados por este spawner.")]
    public int cantidadMaximaSpawn = 3;

    [Tooltip("Velocidad de movimiento de los fuegos buscadores invocados.")]
    public float velocidadFuegoBuscador = 3f;

    [Tooltip("Restricción del eje o plano de movimiento para los fuegos buscadores.")]
    public FuegoBuscador.RestriccionMovimiento restriccionMovimiento = FuegoBuscador.RestriccionMovimiento.SoloPlanoXZ;

    private Transform _jugador;
    private float _temporizadorSpawn = 0f;
    private List<GameObject> _spawnsActivos = new List<GameObject>();

    protected override void Start()
    {
        // Ejecutar inicialización de la clase base Fuego (detección de partículas, etc.)
        base.Start();

        // Buscar al jugador automáticamente en la escena usando su componente oficial
        var controladorJugador = FindFirstObjectByType<FirstPersonController>();
        if (controladorJugador != null)
        {
            _jugador = controladorJugador.transform;
        }
    }

    protected override void Update()
    {
        // Ejecutar la lógica de regeneración de la clase base Fuego
        base.Update();

        if (estaExtinguido) return;

        // Gestión del Spawneo
        if (puedeSpawnearFuegosBuscadores && prefabFuegoBuscador != null && _jugador != null)
        {
            // Filtrar y limpiar de la lista los fuegos pequeños destruidos
            _spawnsActivos.RemoveAll(item => item == null);

            // Calcular distancia al jugador
            float distanciaAlJugador = Vector3.Distance(transform.position, _jugador.position);

            if (distanciaAlJugador <= rangoActivacionSpawn)
            {
                _temporizadorSpawn += Time.deltaTime;

                if (_temporizadorSpawn >= intervaloSpawn)
                {
                    _temporizadorSpawn = 0f;

                    // Si no hemos rebasado la cantidad máxima de fuegos buscadores activos
                    if (_spawnsActivos.Count < cantidadMaximaSpawn)
                    {
                        SpawnearFuegoBuscador();
                    }
                }
            }
            else
            {
                // Disminuir temporizador si el jugador se aleja
                _temporizadorSpawn = Mathf.Max(0f, _temporizadorSpawn - Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Invoca una pequeña flama buscadora y le asocia las físicas y objetivos.
    /// </summary>
    private void SpawnearFuegoBuscador()
    {
        if (prefabFuegoBuscador == null)
        {
            Debug.LogError("[FuegoSpawner] ¡ERROR! prefabFuegoBuscador es NULL. Por favor asígnalo en el Inspector.");
            return;
        }

        GameObject instanciado = Instantiate(prefabFuegoBuscador, transform.position, Quaternion.identity);
        if (instanciado == null) return;

        // Establecer un tamaño pequeño proporcional para el fuego buscador
        instanciado.transform.localScale = Vector3.one * 0.4f;

        // Buscar o agregar el componente buscador
        FuegoBuscador scriptBuscador = instanciado.GetComponent<FuegoBuscador>();
        if (scriptBuscador == null)
        {
            scriptBuscador = instanciado.AddComponent<FuegoBuscador>();
        }

        if (scriptBuscador == null)
        {
            Debug.LogError("[FuegoSpawner] ¡ERROR! No se pudo añadir FuegoBuscador al objeto. Asegúrate de que el script compila.");
            return;
        }

        // Configurar los parámetros heredados de este Spawner
        scriptBuscador.objetivo = _jugador;
        scriptBuscador.velocidad = velocidadFuegoBuscador;
        scriptBuscador.restriccionMovimiento = restriccionMovimiento;

        // Registrar
        _spawnsActivos.Add(instanciado);
    }

    /// <summary>
    /// Extingue este fuego base y limpia automáticamente todas las flamas buscadoras activas que invocó.
    /// </summary>
    public override void ApagarFuego()
    {
        // Ejecutar desvanecimiento y destrucción en la clase base Fuego
        base.ApagarFuego();

        // Extinguir todos los fuegos buscadores dependientes de este spawner
        foreach (var instanciado in _spawnsActivos)
        {
            if (instanciado != null)
            {
                Fuego buscador = instanciado.GetComponent<Fuego>();
                if (buscador != null)
                {
                    buscador.ApagarFuego();
                }
                else
                {
                    Destroy(instanciado, 0.5f);
                }
            }
        }
        _spawnsActivos.Clear();
    }
}
