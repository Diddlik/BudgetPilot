package de.budgetpilot.android

import android.app.Application
import android.util.Log
import dagger.hilt.android.HiltAndroidApp

@HiltAndroidApp
class BudgetPilotApp : Application() {
    override fun onCreate() {
        super.onCreate()
        Log.i("BudgetPilotApp", "Application onCreate called")
    }
}
