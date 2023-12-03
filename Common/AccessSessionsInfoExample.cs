using TradingPlatform.BusinessLayer;

namespace ApiExamples
{
    public class AccessSessionsInfoExample
    {
        public void DisplaySessionsInfo(Symbol symbol)
        {
            if (symbol == null)
                return;

            var sessionsContainer = symbol.FindSessionsContainer();
            if (sessionsContainer == null)
                return;

            foreach (var session in sessionsContainer.ActiveSessions)
            {
                Core.Instance.Loggers.Log($"Session Name: '{session.Name}' Open time: {session.OpenTime} Close time: {session.CloseTime}");

                if (session.ContainsDate(Core.Instance.TimeUtils.DateTimeUtcNow))
                    Core.Instance.Loggers.Log($"Session {session.Name} is active now.");
            }
        }
    }
}