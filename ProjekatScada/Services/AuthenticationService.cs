using System;
using System.Collections.Generic;
using System.Linq;
using ProjekatScada.Data;
using ProjekatScada.Data.Entities;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Services
{
    public class AuthenticationService
    {
        public UserSession Login(string username, string password, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Korisničko ime je obavezno.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Lozinka je obavezna.");
            }

            using (var context = new ScadaDbContext())
            {
                EnsureDatabaseReady(context);

                var user = context.Users.FirstOrDefault(u =>
                    u.Username == username.Trim());

                if (user == null)
                {
                    throw new InvalidOperationException("Korisnik nije pronađen.");
                }

                if (user.Role != role)
                {
                    throw new InvalidOperationException("Izabrana uloga ne odgovara korisniku.");
                }

                if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
                {
                    throw new InvalidOperationException("Pogrešna lozinka.");
                }

                return new UserSession(user.Username, user.Role);
            }
        }

        public UserSession Register(string username, string password, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Korisničko ime je obavezno.");
            }

            var validationErrors = PasswordValidator.Validate(password).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            var passwordHash = PasswordHasher.HashPassword(password);

            using (var context = new ScadaDbContext())
            {
                EnsureDatabaseReady(context);

                if (context.Users.Any(u => u.Username == username.Trim()))
                {
                    throw new InvalidOperationException("Korisničko ime već postoji.");
                }

                foreach (var existingUser in context.Users.ToList())
                {
                    if (PasswordHasher.VerifyPassword(password, existingUser.PasswordHash))
                    {
                        throw new InvalidOperationException("Lozinka već postoji u bazi. Izaberite drugu lozinku.");
                    }
                }

                context.Users.Add(new UserEntity
                {
                    Username = username.Trim(),
                    PasswordHash = passwordHash,
                    Role = role
                });
                context.SaveChanges();

                return new UserSession(username.Trim(), role);
            }
        }

        public static void EnsureDatabaseReady(ScadaDbContext context)
        {
            context.Database.Initialize(false);
        }
    }
}
