public static class ErrorMessageDictionary
{
    /// <summary>
    /// Liste der Fehlermeldungstexte, wobei "SPK-1" dem Index 0 entspricht usw.
    /// </summary>
    private static readonly string[] Messages = new[]
    {
        "Es wurden keine Kontrollberichte empfangen.",
        "Es wurden Kontrollberichte ohne Inspektionsdatum geliefert.",
        "Für diesen Spielplatz ist am selben Tag bereits ein Bericht mit derselben Inspektionsart eingereicht worden.",
        "Interner Server-Fehler.",
        "Es wurden keine Mängel geliefert.",
        "Es wurden keine Spielgeräte geliefert.",
        "Es wurde ein leeres Bild geliefert.",
        "Die Objekt-ID (UUID) fehlt.",
        "Ein Spielgerät, das nicht geprüft werden muss, wurde an den Webservice zur Prüfung gesendet.",
        "Änderung nicht gespeichert, da nicht ausreichend Benutzerdaten angegeben wurden."
    };

    /// <summary>
    /// Gibt den Fehlermeldungstext zum Fehlercode zurück (z. B. "SPK-3").
    /// </summary>
    /// <param name="code">Der Fehlercode im Format "SPK-1", "SPK-2", ...</param>
    /// <returns>Die zugehörige Fehlermeldung oder ein Platzhalter bei ungültigem Code.</returns>
    public static string Get(string code)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            if (code.StartsWith("SPK-"))
            {

                var numberPart = code.Substring(4);

                if (int.TryParse(numberPart, out int index))
                {

                    if (index >= 0 && index < Messages.Length)
                    {
                        return Messages[index];
                    }
                }
            }
            else
            {
                return $"Unbekannter Fehlercode: {code}";

            }
        }
        return "";
    }
}
