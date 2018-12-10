using System;
using System.Configuration;
using System.Linq.Expressions;

namespace OldFileDeleter.ClassLibrary
{
    internal class AppSetting<T, S> where S : ApplicationSettingsBase
    {
        private readonly Expression<Func<S, T>> Selector;
        public readonly T Value;

        public string Name => ((MemberExpression)Selector.Body).Member.Name;

        public AppSetting(S settings, Expression<Func<S, T>> selector)
        {
            Selector = selector;

            try
            {
                Value = Selector.Compile()(settings);
            }
            catch (Exception e)
            {
                throw new FileDeleterException($"Cannot read application setting \"{Name}\".", e);
            }
        }

        public U Select<U>(Func<T, U> selector)
        {
            try
            {
                return selector(Value);
            }
            catch (Exception e)
            {
                throw new FileDeleterException($"Application setting \"{Name}\" has invalid value.", e);
            }
        }

        public AppSetting<T, S> Validate(Func<T, bool> predicate, Func<string, string> messageFunc)
        {
            if (!predicate(Value))
            {
                throw new FileDeleterException(messageFunc($"Application settting \"{Name}\""));
            }

            return this;
        }

        public AppSetting<T, S> IsNotNull()
        {
            return Validate(value => value != null, name => $"{name} cannot be empty.");
        }
    }
}
