using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
        }

        public async Task<CategoryResponseDto> GetCategoryByIdAsync(GetByIdDto dto)
        {
            if (dto == null || dto.Id <= 0)
                throw new ArgumentException("Invalid Id");

            var category = await _unitOfWork.Categories.GetByIdAsync(dto.Id);
            if (category == null)
                throw new NotFoundException("Category", dto.Id);

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto)
        {
            var name = dto.Name.Trim();
            var exists = await _unitOfWork.Categories
                .Query()
                .AnyAsync(c => EF.Functions.Like(c.Name, name));
            if (exists)
                throw new ValidationException("Category already exists");
            var category = _mapper.Map<Category>(dto);
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task UpdateCategoryAsync(UpdateCategoryDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(dto.Id);
            if (category == null)
                throw new NotFoundException("Category", dto.Id);
            var name = dto.Name.Trim();
            var exists = await _unitOfWork.Categories
                .Query()
                .AnyAsync(c => c.Id != dto.Id && EF.Functions.Like(c.Name, name));
            if (exists)
                throw new ValidationException("Category already exists");
            category.Name = dto.Name;
            await _unitOfWork.Categories.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(GetByIdDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(dto.Id);
            if (category == null)
                throw new NotFoundException("Category", dto.Id);

            // FIX: check existence with AnyAsync instead of loading all tickets into memory
            var hasTickets = await _unitOfWork.Tickets
                .Query()
                .AnyAsync(t => t.CategoryId == dto.Id);
            if (hasTickets)
                throw new ValidationException("Cannot delete category with tickets");

            await _unitOfWork.Categories.DeleteAsync(category);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}