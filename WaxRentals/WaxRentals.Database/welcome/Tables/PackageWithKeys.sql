CREATE TABLE [welcome].[PackageWithKeys]
(
	PackageWithKeysId       INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_welcome_PackageWithKeys                         PRIMARY KEY
	,Inserted               DATETIME2(0)               NOT NULL CONSTRAINT DF_welcome_PackageWithKeys__Inserted               DEFAULT GETUTCDATE()
	
	,WaxAccount             CHAR(12)                   NOT NULL
	,OwnerPublicKey         CHAR(53)                   NOT NULL
	,ActivePublicKey        CHAR(53)                   NOT NULL
	,Ram                    INT                        NOT NULL CONSTRAINT CK_welcome_PackageWithKeys__Ram                    CHECK   (Ram                         >= 3000)
    
	,Banano                 DECIMAL(18,8)              NOT NULL CONSTRAINT CK_welcome_PackageWithKeys__Banano                 CHECK   (Banano                      >= 0   )
	,SweepBananoTransaction CHAR(64)                       NULL CONSTRAINT CK_welcome_PackageWithKeys__SweepBananoTransaction CHECK   (LEN(SweepBananoTransaction)  = 64  )
	,Paid                   DATETIME2(0)                   NULL
															  
	,FundTransaction        CHAR(64)                       NULL CONSTRAINT CK_welcome_PackageWithKeys__FundTransaction        CHECK   (LEN(FundTransaction)         = 64  )
	,NftTransaction         CHAR(64)                       NULL CONSTRAINT CK_welcome_PackageWithKeys__NftTransaction         CHECK   (LEN(NftTransaction)          = 64  )
	,RentalId               INT                            NULL CONSTRAINT FK_welcome_PackageWithKeys__Rental                 FOREIGN KEY REFERENCES dbo.Rental (RentalId)

	,StatusId               INT                        NOT NULL CONSTRAINT FK_welcome_PackageWithKeys__Status                 FOREIGN KEY REFERENCES dbo.Status (StatusId)
	                                                            CONSTRAINT DF_welcome_PackageWithKeys__StatusId               DEFAULT 1
)
