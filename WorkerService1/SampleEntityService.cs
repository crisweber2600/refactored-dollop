using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkerService1.Models;
using WorkerService1.Repositories;
using ExampleLib.Domain;

namespace WorkerService1.Services
{
    public interface ISampleEntityService
    {
        Task<(bool Success, int Count)> AddAndCountAsync(string name, double value);
        Task<(bool Success, bool IsValid)> DeleteAndCheckUnvalidatedAsync(int id);
        Task<(bool Success, bool IsValid)> HardDeleteAndCheckRemovedAsync(int id);
        Task<SampleEntity?> GetByIdIncludingDeletedAsync(int id);
        Task<(int Count, int ValidCount, int InvalidCount)> AddManyAndCountAsync(IEnumerable<(string name, double value)> items);
        Task<(bool Success, bool IsValid)> UpdateAndCheckAsync(int id, string newName, double newValue);
        Task<(bool AllUpdated, int ValidCount, int InvalidCount)> UpdateManyAndCheckAsync(Dictionary<int, (string name, double value)> updates);
    }

    public interface IOtherEntityService
    {
        Task<(bool Success, int Count)> AddAndCountAsync(string code, int amount, bool isActive);
        Task<(bool Success, bool IsValid)> DeleteAndCheckUnvalidatedAsync(int id);
        Task<(bool Success, bool IsValid)> HardDeleteAndCheckRemovedAsync(int id);
        Task<OtherEntity?> GetByIdIncludingDeletedAsync(int id);
        Task<(int Count, int ValidCount, int InvalidCount)> AddManyAndCountAsync(IEnumerable<(string code, int amount, bool isActive)> items);
        Task<(bool Success, bool IsValid)> UpdateAndCheckAsync(int id, string newCode, int newAmount, bool newIsActive);
        Task<(bool AllUpdated, int ValidCount, int InvalidCount)> UpdateManyAndCheckAsync(Dictionary<int, (string code, int amount, bool isActive)> updates);
    }

    public class SampleEntityService : ISampleEntityService
    {
        private readonly IRepository<SampleEntity> _repo;
        public SampleEntityService(IRepository<SampleEntity> repo)
        {
            _repo = repo;
        }

        public async Task<(bool Success, int Count)> AddAndCountAsync(string name, double value)
        {
            var entity = new SampleEntity { Name = name, Value = value };
            var isValid = await _repo.ValidateAsync(entity);
            if (!isValid) return (false, 0);
            await _repo.AddAsync(entity);
            var all = await _repo.GetAllAsync();
            return (true, all.Count);
        }

        public async Task<(bool Success, bool IsValid)> DeleteAndCheckUnvalidatedAsync(int id)
        {
            await _repo.DeleteAsync(id);
            var entity = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (entity != null)
                isValid = await _repo.ValidateAsync(entity);
            return (entity == null, isValid);
        }

        public async Task<(bool Success, bool IsValid)> HardDeleteAndCheckRemovedAsync(int id)
        {
            await _repo.DeleteAsync(id);
            var entity = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (entity != null)
                isValid = await _repo.ValidateAsync(entity);
            return (entity == null, isValid);
        }

        public async Task<SampleEntity?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<(int Count, int ValidCount, int InvalidCount)> AddManyAndCountAsync(IEnumerable<(string name, double value)> items)
        {
            int valid = 0, invalid = 0;
            foreach (var (name, value) in items)
            {
                var entity = new SampleEntity { Name = name, Value = value };
                var isValid = await _repo.ValidateAsync(entity);
                if (isValid)
                {
                    await _repo.AddAsync(entity);
                    valid++;
                }
                else
                {
                    invalid++;
                }
            }
            var all = await _repo.GetAllAsync();
            return (all.Count, valid, invalid);
        }

        public async Task<(bool Success, bool IsValid)> UpdateAndCheckAsync(int id, string newName, double newValue)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return (false, false);
            entity.Name = newName;
            entity.Value = newValue;
            await _repo.UpdateAsync(entity);
            var updated = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (updated != null)
                isValid = await _repo.ValidateAsync(updated);
            return (updated?.Name == newName && updated?.Value == newValue, isValid);
        }

        public async Task<(bool AllUpdated, int ValidCount, int InvalidCount)> UpdateManyAndCheckAsync(Dictionary<int, (string name, double value)> updates)
        {
            bool allUpdated = true;
            int valid = 0, invalid = 0;
            foreach (var kvp in updates)
            {
                var entity = await _repo.GetByIdAsync(kvp.Key);
                if (entity == null) { allUpdated = false; continue; }
                entity.Name = kvp.Value.name;
                entity.Value = kvp.Value.value;
                await _repo.UpdateAsync(entity);
            }
            foreach (var kvp in updates)
            {
                var entity = await _repo.GetByIdAsync(kvp.Key);
                if (entity != null && await _repo.ValidateAsync(entity)) valid++;
                else invalid++;
            }
            return (allUpdated, valid, invalid);
        }
    }

    public class OtherEntityService : IOtherEntityService
    {
        private readonly IRepository<OtherEntity> _repo;
        public OtherEntityService(IRepository<OtherEntity> repo)
        {
            _repo = repo;
        }

        public async Task<(bool Success, int Count)> AddAndCountAsync(string code, int amount, bool isActive)
        {
            var entity = new OtherEntity { Code = code, Amount = amount, IsActive = isActive };
            var isValid = await _repo.ValidateAsync(entity);
            if (!isValid) return (false, 0);
            await _repo.AddAsync(entity);
            var all = await _repo.GetAllAsync();
            return (true, all.Count);
        }

        public async Task<(bool Success, bool IsValid)> DeleteAndCheckUnvalidatedAsync(int id)
        {
            await _repo.DeleteAsync(id);
            var entity = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (entity != null)
                isValid = await _repo.ValidateAsync(entity);
            return (entity == null, isValid);
        }

        public async Task<(bool Success, bool IsValid)> HardDeleteAndCheckRemovedAsync(int id)
        {
            await _repo.DeleteAsync(id);
            var entity = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (entity != null)
                isValid = await _repo.ValidateAsync(entity);
            return (entity == null, isValid);
        }

        public async Task<OtherEntity?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<(int Count, int ValidCount, int InvalidCount)> AddManyAndCountAsync(IEnumerable<(string code, int amount, bool isActive)> items)
        {
            int valid = 0, invalid = 0;
            foreach (var (code, amount, isActive) in items)
            {
                var entity = new OtherEntity { Code = code, Amount = amount, IsActive = isActive };
                var isValid = await _repo.ValidateAsync(entity);
                if (isValid)
                {
                    await _repo.AddAsync(entity);
                    valid++;
                }
                else
                {
                    invalid++;
                }
            }
            var all = await _repo.GetAllAsync();
            return (all.Count, valid, invalid);
        }

        public async Task<(bool Success, bool IsValid)> UpdateAndCheckAsync(int id, string newCode, int newAmount, bool newIsActive)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return (false, false);
            entity.Code = newCode;
            entity.Amount = newAmount;
            entity.IsActive = newIsActive;
            await _repo.UpdateAsync(entity);
            var updated = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (updated != null)
                isValid = await _repo.ValidateAsync(updated);
            return (updated?.Code == newCode && updated?.Amount == newAmount && updated?.IsActive == newIsActive, isValid);
        }

        public async Task<(bool AllUpdated, int ValidCount, int InvalidCount)> UpdateManyAndCheckAsync(Dictionary<int, (string code, int amount, bool isActive)> updates)
        {
            bool allUpdated = true;
            int valid = 0, invalid = 0;
            foreach (var kvp in updates)
            {
                var entity = await _repo.GetByIdAsync(kvp.Key);
                if (entity == null) { allUpdated = false; continue; }
                entity.Code = kvp.Value.code;
                entity.Amount = kvp.Value.amount;
                entity.IsActive = kvp.Value.isActive;
                await _repo.UpdateAsync(entity);
            }
            foreach (var kvp in updates)
            {
                var entity = await _repo.GetByIdAsync(kvp.Key);
                if (entity != null && await _repo.ValidateAsync(entity)) valid++;
                else invalid++;
            }
            return (allUpdated, valid, invalid);
        }
    }
}
