-- Golf Store Manager schema (SQLite)
-- Safe to run on every startup: every statement is idempotent.

CREATE TABLE IF NOT EXISTS Products (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Sku             TEXT    NOT NULL UNIQUE,
    Name            TEXT    NOT NULL,
    Brand           TEXT    NOT NULL,
    Category        TEXT    NOT NULL,
    Price           NUMERIC NOT NULL CHECK (Price >= 0),
    QuantityInStock INTEGER NOT NULL DEFAULT 0 CHECK (QuantityInStock >= 0)
);

CREATE TABLE IF NOT EXISTS Customers (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName  TEXT NOT NULL,
    Email     TEXT NOT NULL UNIQUE COLLATE NOCASE,
    Phone     TEXT
);

CREATE TABLE IF NOT EXISTS Orders (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(Id) ON DELETE RESTRICT,
    OrderDate  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    Status     TEXT    NOT NULL DEFAULT 'Pending'
               CHECK (Status IN ('Pending', 'Shipped', 'Delivered', 'Cancelled'))
);

CREATE TABLE IF NOT EXISTS OrderItems (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId   INTEGER NOT NULL REFERENCES Orders(Id)   ON DELETE CASCADE,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE RESTRICT,
    Quantity  INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPrice NUMERIC NOT NULL CHECK (UnitPrice >= 0)
);

CREATE INDEX IF NOT EXISTS IX_Orders_CustomerId    ON Orders(CustomerId);
CREATE INDEX IF NOT EXISTS IX_OrderItems_OrderId   ON OrderItems(OrderId);
CREATE INDEX IF NOT EXISTS IX_OrderItems_ProductId ON OrderItems(ProductId);
