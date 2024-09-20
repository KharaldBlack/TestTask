Постарался выполнить и отладить все пункты, кроме юнит тестов и ленивой подгрузки/постраничного просмотра сотрудников

(пароли AdminPassword123 и UserPassword123)

INSERT INTO Users (Username, PasswordHash, Role)
VALUES ('admin', 'AQAAAAIAAYagAAAAEGslt7jEAKG7IUry7dRoQlA5czQr/WL00ukRL69TxitYnNAv+PilA8v4uMvLaU3meQ==', 'Admin');

INSERT INTO Users (Username, PasswordHash, Role)
VALUES ('user', 'AQAAAAIAAYagAAAAEEWQTQ7bsTcFkY1pEihH64iOI7xCB1cNAXNgiOUbq6TuQBL8nWVeOsT0x8fSsDdE3Q==', 'User');
