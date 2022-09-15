CREATE TABLE [welcome].[AddressWithKeys]
(
	AddressWithKeysId  INT      NOT NULL CONSTRAINT PK_welcome_AddressWithKeys         PRIMARY KEY
	,Address           CHAR(64) NOT NULL CONSTRAINT UQ_welcome_AddressWithKeys_Address UNIQUE (Address)
)
