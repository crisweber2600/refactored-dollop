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

    public class SampleEntityService : ISampleEntityService
    {
        private readonly ISampleRepository<SampleEntity> _repo;
        private readonly IValidationRunner _validationRunner;
        public SampleEntityService(ISampleRepository<SampleEntity> repo, IValidationRunner validationRunner)
        {
            _repo = repo;
            _validationRunner = validationRunner;
        }

        public async Task<(bool Success, int Count)> AddAndCountAsync(string name, double value)
        {
            var entity = new SampleEntity { Name = name, Value = value };
            var isValid = await _validationRunner.ValidateAsync(entity, CancellationToken.None);
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
                isValid = await _validationRunner.ValidateAsync(entity, CancellationToken.None);
            return (entity == null, isValid);
        }

        public async Task<(bool Success, bool IsValid)> HardDeleteAndCheckRemovedAsync(int id)
        {
            await _repo.DeleteAsync(id);
            var entity = await _repo.GetByIdAsync(id);
            bool isValid = false;
            if (entity != null)
                isValid = await _validationRunner.ValidateAsync(entity, CancellationToken.None);
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
                var isValid = await _validationRunner.ValidateAsync(entity, CancellationToken.None);
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
                isValid = await _validationRunner.ValidateAsync(updated, CancellationToken.None);
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
                if (entity != null && await _validationRunner.ValidateAsync(entity, CancellationToken.None)) valid++;
                else invalid++;
            }
            return (allUpdated, valid, invalid);
        }
    }
}
