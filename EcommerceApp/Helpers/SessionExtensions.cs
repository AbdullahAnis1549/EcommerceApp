using System.Text.Json;

namespace EcommerceApp.Helpers
{
    public static class SessionExtensions
    {
        public static void SetJson<T>(this ISession session, string key, T value) =>
            session.SetString(key, JsonSerializer.Serialize(value));

        public static T? GetJson<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json == null ? default : JsonSerializer.Deserialize<T>(json);
        }
    }
}
