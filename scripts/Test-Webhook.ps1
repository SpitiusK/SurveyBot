# Test-Webhook.ps1
# PowerShell script to test the Telegram Bot webhook endpoint

param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:5001",

    [Parameter(Mandatory=$false)]
    [string]$WebhookSecret = "test-secret-token",

    [Parameter(Mandatory=$false)]
    [ValidateSet("start_command", "surveys_command", "help_command", "text_message", "callback_survey_select", "callback_command_help", "unsupported_update_type")]
    [string]$UpdateType = "start_command"
)

# Disable SSL certificate validation for localhost testing
if ($BaseUrl -like "*localhost*") {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Telegram Bot Webhook Tester" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Sample updates
$sampleUpdates = @{
    start_command = @{
        update_id = 123456789
        message = @{
            message_id = 1
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                last_name = "Doe"
                username = "johndoe"
                language_code = "en"
            }
            chat = @{
                id = 123456789
                first_name = "John"
                last_name = "Doe"
                username = "johndoe"
                type = "private"
            }
            date = 1699999999
            text = "/start"
            entities = @(
                @{
                    offset = 0
                    length = 6
                    type = "bot_command"
                }
            )
        }
    }

    surveys_command = @{
        update_id = 123456790
        message = @{
            message_id = 2
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                username = "johndoe"
            }
            chat = @{
                id = 123456789
                type = "private"
            }
            date = 1699999999
            text = "/surveys"
            entities = @(
                @{
                    offset = 0
                    length = 8
                    type = "bot_command"
                }
            )
        }
    }

    help_command = @{
        update_id = 123456791
        message = @{
            message_id = 3
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                username = "johndoe"
            }
            chat = @{
                id = 123456789
                type = "private"
            }
            date = 1699999999
            text = "/help"
            entities = @(
                @{
                    offset = 0
                    length = 5
                    type = "bot_command"
                }
            )
        }
    }

    text_message = @{
        update_id = 123456792
        message = @{
            message_id = 4
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                username = "johndoe"
            }
            chat = @{
                id = 123456789
                type = "private"
            }
            date = 1699999999
            text = "Hello, I need help with surveys"
        }
    }

    callback_survey_select = @{
        update_id = 123456793
        callback_query = @{
            id = "987654321"
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                username = "johndoe"
            }
            message = @{
                message_id = 5
                from = @{
                    id = 987654321
                    is_bot = $true
                    first_name = "SurveyBot"
                    username = "surveybot"
                }
                chat = @{
                    id = 123456789
                    type = "private"
                }
                date = 1699999999
                text = "Available surveys:\n\n1. Customer Satisfaction Survey"
            }
            chat_instance = "123456789"
            data = "survey:1"
        }
    }

    callback_command_help = @{
        update_id = 123456794
        callback_query = @{
            id = "987654322"
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                username = "johndoe"
            }
            message = @{
                message_id = 6
                from = @{
                    id = 987654321
                    is_bot = $true
                    first_name = "SurveyBot"
                    username = "surveybot"
                }
                chat = @{
                    id = 123456789
                    type = "private"
                }
                date = 1699999999
                text = "Main menu"
            }
            chat_instance = "123456789"
            data = "cmd:help"
        }
    }

    unsupported_update_type = @{
        update_id = 123456795
        edited_message = @{
            message_id = 7
            from = @{
                id = 123456789
                is_bot = $false
                first_name = "John"
                username = "johndoe"
            }
            chat = @{
                id = 123456789
                type = "private"
            }
            date = 1699999999
            edit_date = 1700000010
            text = "Corrected message"
        }
    }
}

# Select the update to send
$update = $sampleUpdates[$UpdateType]

Write-Host "Test Configuration:" -ForegroundColor Yellow
Write-Host "  Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "  Update Type: $UpdateType" -ForegroundColor Gray
Write-Host "  Webhook Secret: $WebhookSecret" -ForegroundColor Gray
Write-Host ""

# Prepare request
$webhookUrl = "$BaseUrl/api/bot/webhook"
$headers = @{
    "Content-Type" = "application/json"
    "X-Telegram-Bot-Api-Secret-Token" = $WebhookSecret
}

$body = $update | ConvertTo-Json -Depth 10

Write-Host "Sending webhook request..." -ForegroundColor Yellow
Write-Host ""

try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $response = Invoke-RestMethod -Uri $webhookUrl -Method Post -Headers $headers -Body $body -ErrorAction Stop

    $stopwatch.Stop()

    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response Time: $($stopwatch.ElapsedMilliseconds) ms" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5 | Write-Host

} catch {
    Write-Host "ERROR!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error Message: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.ErrorDetails.Message) {
        Write-Host ""
        Write-Host "Error Details:" -ForegroundColor Yellow
        $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 5 | Write-Host
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan

# Test bot status endpoint
Write-Host ""
Write-Host "Testing bot status endpoint..." -ForegroundColor Yellow

try {
    $statusUrl = "$BaseUrl/api/bot/status"
    $statusResponse = Invoke-RestMethod -Uri $statusUrl -Method Get -ErrorAction Stop

    Write-Host "Bot Status:" -ForegroundColor Green
    $statusResponse | ConvertTo-Json -Depth 5 | Write-Host

} catch {
    Write-Host "Failed to get bot status: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
