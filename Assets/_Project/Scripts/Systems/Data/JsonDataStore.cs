// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Persistencia en JSON (JsonUtility, cero dependencias externas).
/// Estructura en Application.persistentDataPath/AttentiONData/:
///   - profile_&lt;id&gt;.json  → ProfileDatabase (perfil + sesiones + resultados)
///   - tutor_settings.json    → TutorSettings (hash del PIN)
/// Escritura inmediata en cada mutación (los volúmenes de datos son pequeños).
/// </summary>
public class JsonDataStore : IDataStore
{
    const string FOLDER = "AttentiONData";
    const string PROFILE_PREFIX = "profile_";
    const string SETTINGS_FILE = "tutor_settings.json";

    readonly string _root;
    readonly Dictionary<string, ProfileDatabase> _cache = new Dictionary<string, ProfileDatabase>();
    TutorSettings _settings;

    public JsonDataStore()
    {
        _root = Path.Combine(Application.persistentDataPath, FOLDER);
        try { Directory.CreateDirectory(_root); }
        catch (Exception e) { Debug.LogError($"[JsonDataStore] No se pudo crear {_root}: {e.Message}"); }
        LoadAll();
    }

    void LoadAll()
    {
        _cache.Clear();
        try
        {
            foreach (string file in Directory.GetFiles(_root, PROFILE_PREFIX + "*.json"))
            {
                try
                {
                    var db = JsonUtility.FromJson<ProfileDatabase>(File.ReadAllText(file));
                    if (db?.profile != null && !string.IsNullOrEmpty(db.profile.id))
                        _cache[db.profile.id] = db;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[JsonDataStore] Fichero de perfil corrupto ({file}): {e.Message}");
                }
            }

            string sPath = Path.Combine(_root, SETTINGS_FILE);
            _settings = File.Exists(sPath)
                ? JsonUtility.FromJson<TutorSettings>(File.ReadAllText(sPath)) ?? new TutorSettings()
                : new TutorSettings();
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonDataStore] Error cargando datos: {e.Message}");
            _settings = new TutorSettings();
        }
    }

    string PathFor(string profileId) => Path.Combine(_root, PROFILE_PREFIX + profileId + ".json");

    void Persist(string profileId)
    {
        if (!_cache.TryGetValue(profileId, out var db)) return;
        try { File.WriteAllText(PathFor(profileId), JsonUtility.ToJson(db, true)); }
        catch (Exception e) { Debug.LogError($"[JsonDataStore] Error guardando perfil {profileId}: {e.Message}"); }
    }

    ProfileDatabase Db(string profileId)
    {
        _cache.TryGetValue(profileId, out var db);
        return db;
    }

    // ------------------------------------------------ Perfiles

    public List<ProfileData> GetAllProfiles() =>
        _cache.Values.Select(d => d.profile)
              .OrderBy(p => p.fechaCreacionUtcTicks)
              .ToList();

    public ProfileData GetProfile(string profileId) => Db(profileId)?.profile;

    public void SaveProfile(ProfileData profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.id)) return;
        if (!_cache.TryGetValue(profile.id, out var db))
        {
            db = new ProfileDatabase();
            _cache[profile.id] = db;
        }
        db.profile = profile;
        Persist(profile.id);
    }

    public void DeleteProfile(string profileId)
    {
        _cache.Remove(profileId);
        try
        {
            string p = PathFor(profileId);
            if (File.Exists(p)) File.Delete(p);
        }
        catch (Exception e) { Debug.LogError($"[JsonDataStore] Error borrando perfil {profileId}: {e.Message}"); }
    }

    /// <summary>Borra TODA la base de datos de perfiles (todos los ficheros de niño).
    /// Mantiene el PIN del tutor para no bloquear el acceso al área de adultos.</summary>
    public void DeleteAllData()
    {
        _cache.Clear();
        try
        {
            foreach (string file in Directory.GetFiles(_root, PROFILE_PREFIX + "*.json"))
            {
                try { File.Delete(file); }
                catch (Exception e) { Debug.LogWarning($"[JsonDataStore] No se pudo borrar {file}: {e.Message}"); }
            }
            Debug.Log("[JsonDataStore] Base de datos de perfiles borrada por completo.");
        }
        catch (Exception e) { Debug.LogError($"[JsonDataStore] Error borrando la base de datos: {e.Message}"); }
    }

    // ------------------------------------------------ Sesiones

    public void AddSession(SessionData session)
    {
        var db = Db(session?.profileId);
        if (db == null) return;
        db.sessions.Add(session);
        Persist(session.profileId);
    }

    public void UpdateSession(SessionData session)
    {
        var db = Db(session?.profileId);
        if (db == null) return;
        int idx = db.sessions.FindIndex(s => s.id == session.id);
        if (idx >= 0) db.sessions[idx] = session; else db.sessions.Add(session);
        Persist(session.profileId);
    }

    public List<SessionData> GetSessions(string profileId) =>
        Db(profileId)?.sessions.OrderBy(s => s.inicioUtcTicks).ToList() ?? new List<SessionData>();

    // ------------------------------------------------ Resultados

    public void AddResult(MinigameResultData result)
    {
        var db = Db(result?.profileId);
        if (db == null) return;
        db.results.Add(result);
        Persist(result.profileId);
    }

    public List<MinigameResultData> GetResults(string profileId) =>
        Db(profileId)?.results.OrderBy(r => r.fechaUtcTicks).ToList() ?? new List<MinigameResultData>();

    // ------------------------------------------------ Tutor

    public string GetTutorPinHash() => _settings?.pinHash ?? "";

    public void SetTutorPinHash(string hash)
    {
        if (_settings == null) _settings = new TutorSettings();
        _settings.pinHash = hash ?? "";
        SaveSettings();
    }

    // ------------------------------------------------ Modo profesional

    public bool GetProfessionalMode() => _settings != null && _settings.modoProfesional;

    public void SetProfessionalMode(bool enabled)
    {
        if (_settings == null) _settings = new TutorSettings();
        _settings.modoProfesional = enabled;
        SaveSettings();
    }

    // ------------------------------------------------ Consentimiento parental

    public string GetConsentVersion() => _settings?.consentimientoVersion ?? "";

    public void SetConsentVersion(string version)
    {
        if (_settings == null) _settings = new TutorSettings();
        _settings.consentimientoVersion = version ?? "";
        _settings.consentimientoUtcTicks = DataUtils.NowTicks();
        SaveSettings();
    }

    void SaveSettings()
    {
        try { File.WriteAllText(Path.Combine(_root, SETTINGS_FILE), JsonUtility.ToJson(_settings, true)); }
        catch (Exception e) { Debug.LogError($"[JsonDataStore] Error guardando ajustes: {e.Message}"); }
    }
}
