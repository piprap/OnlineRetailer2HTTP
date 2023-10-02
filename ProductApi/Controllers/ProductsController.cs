using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Data;
using ProductApi.Models;
using SharedModels;
using System.Threading.Tasks;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IRepository<Product> repository;
        private readonly IConverter<Product, ProductDto> productConverter;

        public ProductsController(IRepository<Product> repos, IConverter<Product, ProductDto> converter)
        {
            repository = repos;
            productConverter = converter;
        }

        [HttpGet]
        public async Task<IEnumerable<ProductDto>> GetAsync()
        {
            var productDtoList = new List<ProductDto>();
            foreach (var product in await repository.GetAllAsync())
            {
                var productDto = productConverter.Convert(product);
                productDtoList.Add(productDto);
            }
            return productDtoList;
        }

        [HttpGet("{id}", Name = "GetProduct")]
        public async Task<IActionResult> GetAsync(int id)
        {
            var item = await repository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            var productDto = productConverter.Convert(item);
            return new ObjectResult(productDto);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] ProductDto productDto)
        {
            if (productDto == null)
            {
                return BadRequest();
            }

            var product = productConverter.Convert(productDto);
            var newProduct = await repository.AddAsync(product);

            return CreatedAtRoute("GetProduct", new { id = newProduct.Id },
                productConverter.Convert(newProduct));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] ProductDto productDto)
        {
            if (productDto == null || productDto.Id != id)
            {
                return BadRequest();
            }

            var modifiedProduct = await repository.GetAsync(id);

            if (modifiedProduct == null)
            {
                return NotFound();
            }

            modifiedProduct.Name = productDto.Name;
            modifiedProduct.Price = productDto.Price;
            modifiedProduct.ItemsInStock = productDto.ItemsInStock;
            modifiedProduct.ItemsReserved = productDto.ItemsReserved;

            await repository.EditAsync(modifiedProduct);
            return new NoContentResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (await repository.GetAsync(id) == null)
            {
                return NotFound();
            }

            await repository.RemoveAsync(id);
            return new NoContentResult();
        }
    }
}
