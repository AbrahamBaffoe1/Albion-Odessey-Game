#pragma once
#include "Core/OdysseyRules.h"
namespace OdysseyPersistence
{
    bool Save(const Odyssey::State& State);
    bool Load(Odyssey::State& State);
}
