using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GatherUp.Core.DO;
using GatherUp.Core;
using System.Text.Json;
using System.IO;
using System;
using System.Collections.Generic;

namespace GatherUp.API.Services
{
    public enum UserRole { Participant, Manager }

    public record CredentialRecord(string Email, string PasswordHash, UserRole Role, int LinkedId);

    public class CredentialService
    {
        private readonly ConcurrentDictionary<string, CredentialRecord> _store = new();
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<EventManager> _managerRepo;
        private readonly IRepository<Event> _eventRepo;
        private readonly string _filePath;

        public CredentialService(IRepository<Participant> participantRepo, IRepository<EventManager> managerRepo, IRepository<Event> eventRepo)
        {
            _participantRepo = participantRepo;
            _managerRepo = managerRepo;
            _eventRepo = eventRepo;

            _filePath = Path.Combine(AppContext.BaseDirectory, "credentials.json");

            LoadFromDisk();
        }

        public bool HasAnyAccounts() => _store.Count > 0;

        // debug helper
        public IEnumerable<CredentialRecord> GetAllForDebug() => _store.Values.ToArray();

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<CredentialRecord[]>(json);
                if (list == null) return;
                foreach (var r in list) _store[r.Email.ToLowerInvariant()] = r;
            }
            catch
            {
                // ignore errors on load
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var arr = _store.Values.ToArray();
                var json = JsonSerializer.Serialize(arr);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // ignore save errors for demo
            }
        }

        public async Task<bool> RegisterParticipantAsync(string name, string email, string password, int eventId)
        {
            var key = email.ToLowerInvariant();
            if (_store.ContainsKey(key)) return false;

            var hash = Hash(password);
            var p = new Participant(0, name, email);
            await _participantRepo.AddAsync(p);

            // attach to event if provided
            if (eventId != 0)
            {
                var ev = await _eventRepo.GetByIdAsync(eventId);
                if (ev != null)
                {
                    ev.ParticipantIds.Add(p.Id);
                    await _eventRepo.UpdateAsync(ev);
                }
            }

            var rec = new CredentialRecord(email, hash, UserRole.Participant, p.Id);
            var added = _store.TryAdd(key, rec);
            if (added) SaveToDisk();
            return added;
        }

        public async Task<bool> RegisterManagerAsync(string name, string email, string password)
        {
            var key = email.ToLowerInvariant();
            if (_store.ContainsKey(key)) return false;

            var hash = Hash(password);
            var m = new EventManager(0, name, email);
            await _managerRepo.AddAsync(m);

            var rec = new CredentialRecord(email, hash, UserRole.Manager, m.Id);
            var added = _store.TryAdd(key, rec);
            if (added) SaveToDisk();
            return added;
        }

        public Task<CredentialRecord?> ValidateAsync(string email, string password)
        {
            var key = email.ToLowerInvariant();
            if (!_store.TryGetValue(key, out var rec)) return Task.FromResult<CredentialRecord?>(null);
            var hash = Hash(password);
            if (rec.PasswordHash == hash) return Task.FromResult<CredentialRecord?>(rec);
            return Task.FromResult<CredentialRecord?>(null);
        }

        public CredentialRecord? GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            _store.TryGetValue(email.ToLowerInvariant(), out var rec);
            return rec;
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
