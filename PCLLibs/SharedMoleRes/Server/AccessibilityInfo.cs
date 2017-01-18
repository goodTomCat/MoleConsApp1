using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using SharedMoleRes.Server.Surrogates;

namespace SharedMoleRes.Server
{
    public class AccessibilityInfo
    {
        protected List<string> ConstUsersF = new List<string>();
        protected List<string> TempUsersF = new List<string>(5);
        private IReadOnlyCollection<string> _constUsers;
        private IReadOnlyCollection<string> _tempUsers;


        protected AccessibilityInfo()
        {
            _constUsers = new ReadOnlyCollection<string>(ConstUsersF);
            _tempUsers = new ReadOnlyCollection<string>(TempUsersF);
        }
        /// <exception cref="ArgumentOutOfRangeException">maxConstUsers меньше нуля. -or- 
        /// maxTempUsers меньше нуля.</exception>
        public AccessibilityInfo(int maxConstUsers, int maxTempUsers) : this()
        {
            if (maxConstUsers < 0)
                throw new ArgumentOutOfRangeException(nameof(maxConstUsers), maxConstUsers, "maxConstUsers < 0.");
            if (maxTempUsers < 0)
                throw new ArgumentOutOfRangeException(nameof(maxTempUsers), maxTempUsers, "maxTempUsers < 0.");

            MaxConstUsers = maxConstUsers;
            MaxTempUsers = maxTempUsers;
        }


        public bool IsPublicProfile { get; set; }
        public IReadOnlyCollection<string> ConstUsers => _constUsers;
        public IReadOnlyCollection<string> TempUsers => _tempUsers;
        public int MaxConstUsers { get; }
        public int MaxTempUsers { get; }
        //public UserForm UserForm { get; set; }


        /// <exception cref="ArgumentNullException">login == null.</exception>
        /// <exception cref="InvalidOperationException">Постоянных пользователей не может быть больше <see cref="MaxConstUsers"/>.</exception>
        public virtual void AddToConst(string login)
        {
            if (login == null)
                throw new ArgumentNullException(nameof(login)) {Source = GetType().AssemblyQualifiedName};
            if (ConstUsersF.Contains(login))
                return;
            if (ConstUsersF.Count == MaxConstUsers)
                throw new InvalidOperationException($"Постоянных пользователей не может быть больше {MaxConstUsers}.")
                { Source = GetType().AssemblyQualifiedName };

            ConstUsersF.Add(login);
        }
        /// <exception cref="ArgumentNullException">logins == null.</exception>
        /// <exception cref="InvalidOperationException">Постоянных пользователей не может быть больше <see cref="MaxConstUsers"/>.</exception>
        public virtual void AddRangeToConst(string[] logins)
        {
            if (logins == null)
                throw new ArgumentNullException(nameof(logins)) { Source = GetType().AssemblyQualifiedName };
            if (logins.Length + ConstUsersF.Count > MaxConstUsers)
                throw new InvalidOperationException($"Постоянных пользователей не может быть больше {MaxConstUsers}.")
                { Source = GetType().AssemblyQualifiedName };

            foreach (string login in logins)
            {
                if (!ConstUsersF.Contains(login))
                    ConstUsersF.Add(login);
            }
        }
        /// <exception cref="ArgumentNullException">login == null.</exception>
        /// <exception cref="InvalidOperationException">Временных пользователей не может быть больше <see cref="MaxTempUsers"/>.</exception>
        public virtual void AddToTemp(string login)
        {
            if (login == null)
                throw new ArgumentNullException(nameof(login)) { Source = GetType().AssemblyQualifiedName };
            if (TempUsersF.Contains(login))
                return;
            if (TempUsersF.Count == MaxTempUsers)
                throw new InvalidOperationException($"Временных пользователей не может быть больше {MaxTempUsers}.")
                { Source = GetType().AssemblyQualifiedName };

            TempUsersF.Add(login);
        }
        /// <exception cref="ArgumentNullException">logins == null.</exception>
        /// <exception cref="InvalidOperationException">Временных пользователей не может быть больше <see cref="MaxTempUsers"/>.</exception>
        public virtual void AddRangeToTemp(string[] logins)
        {
            if (logins == null)
                throw new ArgumentNullException(nameof(logins)) { Source = GetType().AssemblyQualifiedName };
            if (logins.Length + TempUsersF.Count > MaxTempUsers)
                throw new InvalidOperationException($"Временных пользователей не может быть больше {MaxTempUsers}.")
                { Source = GetType().AssemblyQualifiedName };

            foreach (string login in logins)
            {
                if (!TempUsersF.Contains(login))
                    TempUsersF.Add(login);
            }
        }
        public virtual void RemoveFromConst(string login)
        {
            if (login == null)
                return;
            if (ConstUsersF.Count == 0)
                return;

            ConstUsersF.Remove(login);
        }
        public virtual void RemoveRangeFromConst(string[] logins)
        {
            if (logins == null)
                return;
            if (ConstUsersF.Count == 0)
                return;
            if (logins.Length == 0)
                return;

            foreach (string login in logins)
                ConstUsersF.Remove(login);
        }
        public void ClearConst()
        {
            ConstUsersF.Clear();
        }
        public virtual void RemoveFromTemp(string login)
        {
            if (login == null)
                return;
            if (TempUsersF.Count == 0)
                return;

            TempUsersF.Remove(login);
        }
        public virtual void RemoveRangeFromTemp(string[] logins)
        {
            if (logins == null)
                return;
            if (TempUsersF.Count == 0)
                return;
            if (logins.Length == 0)
                return;

            foreach (string login in logins)
                TempUsersF.Remove(login);
        }
        public void ClearTemp()
        {
            TempUsersF.Clear();
        }
        public static implicit operator AccessibilityInfoSur(AccessibilityInfo info)
        {
            if (info == null)
                return null;

            return new AccessibilityInfoSur()
            {
                ConstUsers = string.Join(";", info.ConstUsersF),
                IsPublicProfile = info.IsPublicProfile,
                MaxTempUsers = info.MaxTempUsers,
                MaxConstUsers = info.MaxConstUsers,
                TempUsers = string.Join(";", info.TempUsersF),
            };
        }
        public static implicit operator AccessibilityInfo(AccessibilityInfoSur sur)
        {
            if (sur == null)
                return null;

            var info = new AccessibilityInfo(sur.MaxConstUsers, sur.MaxTempUsers);
            info.IsPublicProfile = sur.IsPublicProfile;
            info.ConstUsersF = new List<string>();
            if (sur.ConstUsers.Length > 0)
            {
                var constUsers = sur.ConstUsers.Split(';');
                if (constUsers.Length > sur.MaxConstUsers)
                {
                    var str = new StringBuilder();
                    str.AppendLine("Количество постоянных пользователей не может быть больше максимального.");
                    str.AppendLine($"sur.ConstUsers.Count: {constUsers.Length}.");
                    str.AppendLine($"sur.MaxConstUsers: {sur.MaxConstUsers}.");
                    throw new InvalidCastException(str.ToString())
                        {Source = typeof(AccessibilityInfo).AssemblyQualifiedName};
                }
                info.ConstUsersF = new List<string>();
                info.ConstUsersF.AddRange(constUsers);
            }

            info.TempUsersF = new List<string>();
            if (sur.TempUsers.Length > 0)
            {
                var tempUsers = sur.TempUsers.Split(';');
                if (tempUsers.Length > sur.MaxTempUsers)
                {
                    var str = new StringBuilder();
                    str.AppendLine("Количество временных пользователей не может быть больше максимального.");
                    str.AppendLine($"sur.TempUsers.Count : {tempUsers.Length}.");
                    str.AppendLine($"sur.MaxTempUsers: {sur.MaxTempUsers}.");
                    throw new InvalidCastException(str.ToString())
                    { Source = typeof(AccessibilityInfo).AssemblyQualifiedName };
                }
                info.TempUsersF.AddRange(tempUsers);
            }
            
            return info;
        }
    }
}
