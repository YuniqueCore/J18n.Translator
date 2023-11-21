namespace J18n.Test.TestData.Json;

public static class JsonData
{
    public const string en_US = /*lang=json,strict*/ """
        {
            "title": "Random Data",
            "description": "This is a randomly generated JSON data.",
            "nestedObject": {
                "name": "John Doe",
                "age": "25"
            },
            "items": [
                "Item 1",
                "Item 2",
                "Item 3"
            ],
            "specialCharacters": "!@#$%^&*()",
            "specialCharacters2": "!@#(hello)$%weoroldf 及法律途径 ^&*(",
            "message": "Hello {{name}}! How are you today?",
            "menuItems": [
            "Home",
            "Products",
            "Services",
            "Contact"
            ]
        }
        """;

    public const string zh_CN = /*lang=json,strict*/ """
        {
            "title": "随机数据",
            "description": "这是一段随机生成的 JSON 数据。",
            "nestedObject": {
                "name": "张三",
                "age": "25 岁"
            },
            "items": [
                "物品 1",
                "物品 2",
                "物品 3"
            ],
            "specialCharacters": "!@#$%^&*()",
            "specialCharacters2": "!@#(你好)$%weoroldf and the way of law ^&*(",
            "message": "你好，{{name}}！你今天好吗？",
            "menuItems": [
            "主页",
            "产品",
            "服务",
            "联系我们"
            ]
        }
        """;

    public const string test_Nested = /*lang=json,strict*/  """
        {
        "title": "Random Data",
        "description": "This is a randomly generated JSON data.",
        "users": [
            {
                "id": 1,
                "name": "John Doe",
                "email": "john.doe@example.com",
                "password": "$2b$10$g/JSk4sRJiZdyHSyE6aBleC9JvL4sA7TUD/g.LAFV9lYXr7hr048m",
                "dateOfBirth": "1996-12-01",
                "address": {
                    "street": "123 Main St",
                    "city": "Anytown",
                    "state": "NY",
                    "zipCode": "12345"
                }
            },
            {
                "id": 2,
                "name": "Jane Smith",
                "email": "jane.smith@example.com",
                "password": "$2b$10$fqkU1u1LVUeORFv.w3glxOQscOnifGKucVpBZt1MzPMDaqyP62d8C",
                "dateOfBirth": "1980-0½-01",
                "address": {
                    "street": "456 Elm St",
                    "city": "Othertown",
                    "state": "CA",
                    "zipCode": "67890"
                }
            }
        ],
        "orders": [
            {
                "id": 1,
                "userId": 1,
                "orderDate": "2022-01-01",
                "totalPrice": 199.99,
                "products": [
                    {
                        "id": 1,
                        "name": "Product 1",
                        "price": 99.99,
                        "quantity": 2
                    },
                    {
                        "id": 2,
                        "name": "Product 2",
                        "price": 49.99,
                        "quantity": 1
                    }
                ],
                "customerInfo": {
                    "name": "John Doe",
                    "email": "john.doe@example.com",
                    "address": {
                        "street": "Main Street",
                        "city": "New York",
                        "postalCode": "10001"
                    }
                }
            },
            {
                "id": 2,
                "userId": 2,
                "orderDate": "2022-02-01",
                "totalPrice": 129.99,
                "products": [
                    {
                        "id": 3,
                        "name": "Product 3",
                        "price": 59.99,
                        "quantity": 2
                    },
                    {
                        "id": 4,
                        "name": "Product 4",
                        "price": 39.99,
                        "quantity": 1
                    }
                ],
                "customerInfo": {
                    "name": "Jane Doe",
                    "email": "jane.doe@example.com",
                    "address": {
                        "street": "Secondary Street",
                        "city": "Los Angeles",
                        "postalCode": "90001"
                    }
                }
            }
        ],
        "shippingInfo": {
            "shippingMethod": "Standard",
            "shippingCost": 9.99,
            "deliveryDate": "2022-03-01"
        },
        "specialCharacters": "!@#$%^&*()",
        "specialCharacters2": "!@#(hello)$%weoroldf 及法律途径 ^&*(",
        "messages": [
            {"id": 1, "text": "Hello {name}! How are you today?", "type": "greeting"},
            {"id": 2, "text": "Goodbye {name}!", "type": "farewell"}
        ],
        "menuItems": [
            {"id": 1, "text": "Home", "url": "/home"},
            {"id": 2, "text": "Products", "url": "/products"},
            {"id": 3, "text": "Services", "url": "/services"},
            {"id": 4, "text": "Contact", "url": "/contact"}
        ],
        "messages": [
            {"id": 1, "text": "{msg}", "type": "greeting"},
            {"id": 2, "text": "{msg}", "type": "farewell"}
        ],
        "fmtStrings": {
            "info": {
                "template": "Info: This is an info message. {msg}",
                "example": "Info: This is an info message. The system has been updated successfully."
            },
            "warning": {
                "template": "Warning: This is a warning message. {msg}",
                "example": "Warning: This is a warning message. Your account will expire in 3 days."
            },
            "error": {
                "template": "Error: This is an error message. {msg}",
                "example": "Error: This is an error message. The server encountered an unexpected error."
            },
            "success": {
                "template": "Success: This is a success message. {msg}",
                "example": "Success: This is a success message. Your order has been placed successfully."
            },
            "exportMsg": {
                "template": "Exporting data...\n User name:{username}, Phone:{phone}, Email:{email}\n Thank your support",
                "example": "Exporting data...\n User name:John Doe, Phone:1234567890, Email:XXXXXXXXXXXXXXXXXXXX\n Thank your support"
            },
            "importMsg": {
                "template": "Importing data...\n User name:{username}, Phone:{phone}, Email:{email}\n Thank your support",
                "example": "Importing data...\n User name:Jane Smith, Phone:9876543210, Email:YYYYYYYYYYYYYYYYYY\n Thank your support"
            },
            "deleteMsg": {
                "template": "Deleting data...\n User name:{username}, Phone:{phone}, Email:{email}\n Thank your support",
                "example": "Deleting data...\n User name:Alice Johnson, Phone:5555555555, Email:ZZZZZZZZZZZZZZZZZ\n Thank your support"
            },
            "updateMsg": {
                "template": "Updating data...\n User name:{username}, Phone:{phone}, Email:{email}\n Thank your support",
                "example": "Updating data...\n User name:Bob Brown, Phone:1111111111, Email:AAAAAAAAAAAAAAAA\n Thank your support"
            }
        }
        }
        """;
}