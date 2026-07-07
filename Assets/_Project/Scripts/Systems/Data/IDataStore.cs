// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;

/// <summary>
/// Abstracción de persistencia. Implementación actual: JsonDataStore (ficheros JSON
/// en Application.persistentDataPath). Se puede sustituir por SQLite implementando
/// esta interfaz sin tocar el resto de sistemas.
/// </summary>
public interface IDataStore
{
    // Perfiles
    List<ProfileData> GetAllProfiles();
    ProfileData GetProfile(string profileId);
    void SaveProfile(ProfileData profile);
    void DeleteProfile(string profileId);          // borra perfil + sesiones + resultados
    void DeleteAllData();                          // borra TODOS los perfiles (base de datos completa)

    // Sesiones
    void AddSession(SessionData session);
    void UpdateSession(SessionData session);
    List<SessionData> GetSessions(string profileId);

    // Resultados de minijuegos
    void AddResult(MinigameResultData result);
    List<MinigameResultData> GetResults(string profileId);

    // Ajustes del tutor (PIN global)
    string GetTutorPinHash();
    void SetTutorPinHash(string hash);

    // Modo profesional (gabinetes: perfiles ilimitados, búsqueda, exportación por lote)
    bool GetProfessionalMode();
    void SetProfessionalMode(bool enabled);

    // Consentimiento parental (versión aceptada de la política; "" = pendiente)
    string GetConsentVersion();
    void SetConsentVersion(string version);
}
