namespace SportSys.Model;

public record class SportLocation(
  string Name,
  string Street,
  string ZipCode,
  string City,
  double Lat,
  double Lon
);
