using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Hatch.Utils;

namespace PatchZone.Hatch
{
    public interface IPatchZoneContext
    {
        ModLog Log { get; }

        void RegisterServicePatch(Type service, Type patchImplementation);
    }

    public struct ModLog
    {
        public enum LogLevel
        {
            Debug,
            Normal,
            Error
        }

        public IPrinter Error;
        public IPrinter Debug;
        public IPrinter Normal;

        public LogLevel CurrentLogLevel;

        public void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            if (logLevel < this.CurrentLogLevel)
                return;

            IPrinter logger;
            switch (logLevel)
            {
                case LogLevel.Debug:
                    logger = this.Debug;
                    break;

                case LogLevel.Normal:
                    logger = this.Normal;
                    break;

                case LogLevel.Error:
                    logger = this.Error;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }

            logger.PrintLine(message);
        }
    }

    public static class PatchZoneContextExtensions
    {
        public static void RegisterProxyService<TService, TProxyService>(this IPatchZoneContext context)
        {
            context.RegisterServicePatch(typeof(TService), typeof(TProxyService));
        }
    }
}
