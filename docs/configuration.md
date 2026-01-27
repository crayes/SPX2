# Configuration

## Delta settings

The worker binds options from the `Delta` configuration section.

Current settings (see `DeltaOptions`):

- `Delta:Enabled` (bool, default `false`)
- `Delta:PollIntervalSeconds` (int, default `5`, range `1..3600`)

### appsettings example

```json
{
  "Delta": {
    "Enabled": true,
    "PollIntervalSeconds": 5
  }
}
```

### Environment variables

.NET maps `:` to `__` in environment variables:

- `Delta__Enabled=true`
- `Delta__PollIntervalSeconds=15`

### Validation behavior

Options use DataAnnotations validation. Invalid values cause the host to fail startup.
