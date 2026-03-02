using AutoMapper;
using BusinessLogicLayer.DTO;
using BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using OrdersMicroservice.API.Entities;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;

public class OrdersService : IOrdersService
{
    private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
    private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
    private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
    private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
    private readonly IMapper _mapper;
    private IOrdersRepository _ordersRepository;
    private readonly UsersMicroserviceClient _userMicroserviceClient;
    private readonly ProductsMicroserviceClient _productsMicroserviceClient;

    public OrdersService(
        IOrdersRepository ordersRepository,
        IMapper mapper,
        IValidator<OrderAddRequest> orderAddRequestValidator,
        IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
        IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
        IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator,
        UsersMicroserviceClient userMicroserviceClient,
        ProductsMicroserviceClient productsMicroserviceClient)
    {
        _orderAddRequestValidator = orderAddRequestValidator;
        _orderItemAddRequestValidator = orderItemAddRequestValidator;
        _orderUpdateRequestValidator = orderUpdateRequestValidator;
        _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
        _mapper = mapper;
        _ordersRepository = ordersRepository;
        _userMicroserviceClient = userMicroserviceClient;
        _productsMicroserviceClient = productsMicroserviceClient;
    }


    public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
    {
        //Check for null parameter
        if (orderAddRequest == null)
        {
            throw new ArgumentNullException(nameof(orderAddRequest));
        }


        //Validate OrderAddRequest using Fluent Validations
        ValidationResult orderAddRequestValidationResult = await _orderAddRequestValidator.ValidateAsync(orderAddRequest);
        if (!orderAddRequestValidationResult.IsValid)
        {
            string errors = string.Join(", ", orderAddRequestValidationResult.Errors.Select(temp => temp.ErrorMessage));
            throw new ArgumentException(errors);
        }

        List<ProductDTO?> products = new List<ProductDTO?>();
        //Validate order items using Fluent Validation
        foreach (OrderItemAddRequest orderItemAddRequest in orderAddRequest.OrderItems)
        {
            ValidationResult orderItemAddRequestValidationResult = await _orderItemAddRequestValidator.ValidateAsync(orderItemAddRequest);

            if (!orderItemAddRequestValidationResult.IsValid)
            {
                string errors = string.Join(", ", orderItemAddRequestValidationResult.Errors.Select(temp => temp.ErrorMessage));
                throw new ArgumentException(errors);
            }

            // Check if ProductID exists in Products microservice. If ProductID does not exist, it will throw an exception which will be handled by the global exception handler middleware.
            ProductDTO product = await _productsMicroserviceClient.GetProductByID(orderItemAddRequest.ProductID);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {orderItemAddRequest.ProductID} does not exist.");
            }
            products.Add(product);
        }

        // Get user details from Users microservice to check if UserID exists. If UserID does not exist, it will throw an exception which will be handled by the global exception handler middleware.
        UserDTO? userDTO = await _userMicroserviceClient.GetUserByID(orderAddRequest.UserID);
        if (userDTO == null)
        {
            throw new ArgumentException($"User with ID {orderAddRequest.UserID} does not exist.");
        }

        //Convert data from OrderAddRequest to Order
        Order orderInput = _mapper.Map<Order>(orderAddRequest); //Map OrderAddRequest to 'Order' type (it invokes OrderAddRequestToOrderMappingProfile class)

        //Generate values
        foreach (OrderItem orderItem in orderInput.OrderItems)
        {
            orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
        }
        orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);


        //Invoke repository
        Order? addedOrder = await _ordersRepository.AddOrder(orderInput);

        if (addedOrder == null)
        {
            return null;
        }

        OrderResponse addedOrderResponse = _mapper.Map<OrderResponse>(addedOrder); //Map addedOrder ('Order' type) into 'OrderResponse' type (it invokes OrderToOrderResponseMappingProfile).

        foreach (OrderItemResponse orderItemResponse in addedOrderResponse.OrderItems)
        {
            ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByID(orderItemResponse.ProductID);

            if (productDTO == null)
                continue;

            _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
        }

        if (addedOrderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in addedOrderResponse.OrderItems)
            {
                ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        // Load UserPersonName and Email from Users microservice
        if (userDTO == null)
        {
            _mapper.Map<UserDTO, OrderResponse>(userDTO, addedOrderResponse);
        }

        return addedOrderResponse;
    }



    public async Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest)
    {
        //Check for null parameter
        if (orderUpdateRequest == null)
        {
            throw new ArgumentNullException(nameof(orderUpdateRequest));
        }


        //Validate OrderAddRequest using Fluent Validations
        ValidationResult orderUpdateRequestValidationResult = await _orderUpdateRequestValidator.ValidateAsync(orderUpdateRequest);
        if (!orderUpdateRequestValidationResult.IsValid)
        {
            string errors = string.Join(", ", orderUpdateRequestValidationResult.Errors.Select(temp => temp.ErrorMessage));
            throw new ArgumentException(errors);
        }

        List<ProductDTO?> products = new List<ProductDTO?>();
        //Validate order items using Fluent Validation
        foreach (OrderItemUpdateRequest orderItemUpdateRequest in orderUpdateRequest.OrderItems)
        {
            ValidationResult orderItemUpdateRequestValidationResult = await _orderItemUpdateRequestValidator.ValidateAsync(orderItemUpdateRequest);

            if (!orderItemUpdateRequestValidationResult.IsValid)
            {
                string errors = string.Join(", ", orderItemUpdateRequestValidationResult.Errors.Select(temp => temp.ErrorMessage));
                throw new ArgumentException(errors);
            }

            // Check if ProductID exists in Products microservice. If ProductID does not exist, it will throw an exception which will be handled by the global exception handler middleware.
            ProductDTO product = await _productsMicroserviceClient.GetProductByID(orderItemUpdateRequest.ProductID);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {orderItemUpdateRequest.ProductID} does not exist.");
            }
            products.Add(product);
        }

        UserDTO? userDTO = await _userMicroserviceClient.GetUserByID(orderUpdateRequest.UserID);
        if (userDTO == null)
        {
            throw new ArgumentException($"User with ID {orderUpdateRequest.UserID} does not exist.");
        }


        //Convert data from OrderUpdateRequest to Order
        Order orderInput = _mapper.Map<Order>(orderUpdateRequest); //Map OrderUpdateRequest to 'Order' type (it invokes OrderUpdateRequestToOrderMappingProfile class)

        //Generate values
        foreach (OrderItem orderItem in orderInput.OrderItems)
        {
            orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
        }
        orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);


        //Invoke repository
        Order? updatedOrder = await _ordersRepository.UpdateOrder(orderInput);

        if (updatedOrder == null)
        {
            return null;
        }

        OrderResponse updatedOrderResponse = _mapper.Map<OrderResponse>(updatedOrder); //Map updatedOrder ('Order' type) into 'OrderResponse' type (it invokes OrderToOrderResponseMappingProfile).

        if (updatedOrderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in updatedOrderResponse.OrderItems)
            {
                ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        // Load UserPersonName and Email from Users microservice
        if (userDTO == null)
        {
            _mapper.Map<UserDTO, OrderResponse>(userDTO, updatedOrderResponse);
        }

        return updatedOrderResponse;
    }


    public async Task<bool> DeleteOrder(Guid orderID)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);
        Order? existingOrder = await _ordersRepository.GetOrderByCondition(filter);

        if (existingOrder == null)
        {
            return false;
        }


        bool isDeleted = await _ordersRepository.DeleteOrder(orderID);
        return isDeleted;
    }


    public async Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        Order? order = await _ordersRepository.GetOrderByCondition(filter);
        if (order == null)
            return null;

        OrderResponse orderResponse = _mapper.Map<OrderResponse>(order);

        foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
        {
            ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByID(orderItemResponse.ProductID);

            if (productDTO == null)
                continue;

            _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
        }

        // Load UserPersonName and Email from Users microservice
        if (orderResponse != null)
        {
            UserDTO? userDTO = await _userMicroserviceClient.GetUserByID(orderResponse.UserID);

            if (userDTO == null)
            {
                _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
            }
        }

        return orderResponse;
    }


    public async Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByCondition(filter);


        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);

        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByID(orderItemResponse.ProductID);

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        // Load UserPersonName and Email from Users microservice
        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            UserDTO? userDTO = await _userMicroserviceClient.GetUserByID(orderResponse.UserID);

            if (userDTO == null)
                continue;

            _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
        }

        return orderResponses.ToList();
    }


    public async Task<List<OrderResponse?>> GetOrders()
    {
        IEnumerable<Order?> orders = await _ordersRepository.GetOrders();


        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);

        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByID(orderItemResponse.ProductID);

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        // Load UserPersonName and Email from Users microservice
        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            UserDTO? userDTO = await _userMicroserviceClient.GetUserByID(orderResponse.UserID);

            if (userDTO == null)
                continue;

            _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
        }

        return orderResponses.ToList();
    }
}