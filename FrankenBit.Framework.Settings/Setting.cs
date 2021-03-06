using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NLog;

namespace FrankenBit.Framework.Settings
{
    internal class Setting
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly object _settings;
        private readonly PropertyInfo _property;

        public Setting( [NotNull] object settings, [NotNull] PropertyInfo property )
        {
            Contract.Requires<ArgumentNullException>( settings != null );
            Contract.Requires<ArgumentNullException>( property != null );

            _settings = settings;
            _property = property;
            Default = Value;
        }

        public bool Changed
        {
            [DebuggerStepThrough]
            get
            {
                if ( Value is float || Value is double )
                {
                    return Math.Abs( (float)Value - (float)Default ) > float.Epsilon;
                }

                return !Equals( Value, Default );
            }
        }

        [NotNull]
        public string DeclaringType
        {
            [DebuggerStepThrough]
            get { return _property.DeclaringType != null ? _property.DeclaringType.FullName : String.Empty; }
        }

        public object Default { get; set; }

        [NotNull]
        public string Name
        {
            [DebuggerStepThrough]
            get { return _property.Name; }
        }

        [NotNull]
        public Type Type
        {
            [DebuggerStepThrough]
            get { return _property.PropertyType; }
        }

        [CanBeNull]
        public object Value
        {
            [DebuggerStepThrough]
            get { return _property.GetValue( _settings ); }
            [DebuggerStepThrough]
            set
            {
                try
                {
                    if ( value != null && value.GetType() != Type )
                    {
                        value = Convert.ChangeType( value, Type );
                    }

                    _property.SetValue( _settings, value );
                }
                catch ( Exception e )
                {
                    Logger.WarnException(
                        string.Format( "Unable to set new value '{0}' for '{1}: {2}'.", value, this, e ), e );
                }
            }
        }

		[NotNull]
		public static IEnumerable<Setting> Scan( [NotNull] object instance )
		{
			Contract.Requires( instance != null );
			Contract.Ensures( Contract.Result<IEnumerable<Setting>>() != null );

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
			Type settingsType = typeof( Settings );

			return instance.GetType().GetProperties( flags )
				.Where( p => p.CanWrite && p.DeclaringType != settingsType )
				.Select( p => new Setting( instance, p ) );
		}

        public void Reload()
        {
            Default = Value;
        }

        public void Reset()
        {
            Value = Default;
        }

        public override string ToString()
        {
            return string.Format( "{0} {1}.{2}{4} = {3}",
                Type, DeclaringType, Name, Value, Changed ? "*" : string.Empty );
        }
    }
}