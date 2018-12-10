using System;
using System.Configuration;
using System.Linq.Expressions;

namespace OldFileDeleter.ClassLibrary
{
    internal class AppSettingReader<S> where S : ApplicationSettingsBase
    {
        private readonly S Settings;

        public AppSettingReader(S settings)
        {
            Settings = settings;
        }

        public AppSetting<T, S> GetAppSetting<T>(Expression<Func<S, T>> selector)
        {
            return new AppSetting<T, S>(Settings, selector);
        }
    }
}
