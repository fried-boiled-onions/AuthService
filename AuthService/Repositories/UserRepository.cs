using Npgsql;

namespace AuthService.Data;

    public class UserRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public UserRepository(string connectionString)
        {
            _dataSource = NpgsqlDataSource.Create(connectionString);
        }

        public async Task<int> AddUserAsync(string username, string email, string password)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                Console.WriteLine("SUCCEED");
                await using var connection = await _dataSource.OpenConnectionAsync();
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "select create_user(@username, @email, @password)";
                cmd.Parameters.Add(new NpgsqlParameter("username", username));
                cmd.Parameters.Add(new NpgsqlParameter("email", email));
                cmd.Parameters.Add(new NpgsqlParameter("password", password));

                try
                {
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch (PostgresException ex) when (ex.SqlState == "23505")
                {
                    throw;
                }
                catch (PostgresException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            
        }

        public async Task UpdateUserAsync(int user_id, string new_username)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "select update_user(@user_id, @new_username)";
            cmd.Parameters.Add(new NpgsqlParameter("user_id", user_id));
            cmd.Parameters.Add(new NpgsqlParameter("new_username", new_username));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> LoginUserAsync(string email, string password)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "select login_user(@email, @password)";
            cmd.Parameters.Add(new NpgsqlParameter("email", email));
            cmd.Parameters.Add(new NpgsqlParameter("password", password));

            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }
    }
