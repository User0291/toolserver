using MySql.Data.MySqlClient;

namespace MainWeb
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("YourDatabaseConnection");
        }
        public async Task UpdateUserAsync(User user)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "UPDATE Users SET DeviceId = @DeviceId, IsUsed = @IsUsed, ExpiryDate = @ExpiryDate WHERE Id = @Id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DeviceId", user.DeviceId);
                    command.Parameters.AddWithValue("@IsUsed", user.IsUsed);
                    command.Parameters.AddWithValue("@ExpiryDate", user.ExpiryDate);
                    command.Parameters.AddWithValue("@Id", user.Id);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<User> GetUserByLicenseKeyAsync(string licenseKey)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT Id, `Key`, DeviceId, IsUsed, ExpiryDate FROM Users WHERE `Key` = @LicenseKey";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LicenseKey", licenseKey);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Key = reader.GetString(1),
                                DeviceId = reader.GetString(2),
                                IsUsed = reader.GetBoolean(3),
                                ExpiryDate = reader.GetDateTime(4)
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}
