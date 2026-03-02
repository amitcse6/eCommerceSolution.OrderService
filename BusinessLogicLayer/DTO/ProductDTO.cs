using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record ProductDTO(Guid ProductID, string ProductName, string? Category, decimal UnitPrice, int Quantity, decimal TotalPrice);