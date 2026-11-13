-- Sample data, loaded only when the database is empty.

INSERT INTO Products (Sku, Name, Brand, Category, Price, QuantityInStock) VALUES
    ('DRV-001', 'Stealth 460cc Driver',          'Apex',      'Drivers',   449.99,  8),
    ('DRV-002', 'Launch Max Driver',             'Fairline',  'Drivers',   379.99,  5),
    ('IRN-001', 'Tour Forged Iron Set (4-PW)',   'Apex',      'Irons',     999.99,  3),
    ('IRN-002', 'Game Improvement Irons (5-PW)', 'Birchwood', 'Irons',     649.99,  6),
    ('WDG-001', '56 Degree Sand Wedge',          'Fairline',  'Wedges',    149.99, 12),
    ('PTR-001', 'Mallet Putter 34"',             'Greenside', 'Putters',   229.99,  7),
    ('BAL-001', 'Tour Soft Golf Balls (Dozen)',  'Apex',      'Balls',      49.99, 60),
    ('BAL-002', 'Distance Golf Balls (Dozen)',   'Birchwood', 'Balls',      29.99,  4),
    ('BAG-001', 'Lightweight Stand Bag',         'Greenside', 'Bags',      219.99,  9),
    ('GLV-001', 'Cabretta Leather Glove (M)',    'Fairline',  'Gloves',     24.99, 35),
    ('SHO-001', 'Spikeless Golf Shoes (Size 10)','Birchwood', 'Footwear',  139.99,  2),
    ('ACC-001', 'Laser Rangefinder',             'Greenside', 'Accessories',299.99, 4);

INSERT INTO Customers (FirstName, LastName, Email, Phone) VALUES
    ('Jordan', 'Reyes',  'jordan.reyes@example.com',  '555-0142'),
    ('Casey',  'Nguyen', 'casey.nguyen@example.com',  '555-0178'),
    ('Morgan', 'Patel',  'morgan.patel@example.com',  NULL);

INSERT INTO Orders (CustomerId, OrderDate, Status) VALUES
    (1, '2026-11-01 10:15:00', 'Delivered'),
    (2, '2026-11-08 14:42:00', 'Shipped'),
    (1, '2026-11-12 09:05:00', 'Pending');

INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice) VALUES
    (1, 1, 1, 449.99),
    (1, 7, 2,  49.99),
    (2, 6, 1, 229.99),
    (2, 10, 3, 24.99),
    (3, 5, 1, 149.99);
